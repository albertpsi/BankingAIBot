"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";
import {
  createTransaction,
  getOverview,
  getSavedPrompts,
  getSession,
  getSessions,
  getApiBase,
  login,
  register,
  savePrompt,
  sendChat,
} from "@/lib/api";
import type {
  BankingSnapshot,
  ChatMessage,
  ChatSessionDetail,
  ChatSessionSummary,
  LoginResponse,
  SavedPrompt,
  Transaction,
} from "@/lib/types";

type AuthMode = "login" | "register";
type Tab = "overview" | "assistant" | "activity";

type ChatRow = {
  role: "user" | "assistant";
  content: string;
  meta?: string;
};

const AUTH_STORAGE_KEY = "banking-aibot.auth";

const moneyFormatter = new Intl.NumberFormat("en-IN", {
  style: "currency",
  currency: "INR",
  maximumFractionDigits: 2,
});

function formatMoney(value: number) {
  return moneyFormatter.format(value);
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

function formatConversationMessage(message: ChatMessage) {
  const metaBits = [message.toolName, message.modelName]
    .filter(Boolean)
    .join(" · ");
  const normalRole = message.role.trim().toLowerCase();
  // Filter out tool and system messages – only show user/assistant
  if (normalRole !== "user" && normalRole !== "assistant") return null;
  return {
    role: normalRole === "user" ? "user" : "assistant",
    content: message.content,
    meta: metaBits || undefined,
  } as ChatRow;
}

function renderFormattedText(text: string) {
  const lines = text.split(/\r?\n/);
  const blocks: ReactNode[] = [];
  let listItems: string[] = [];

  const flushList = (key: number) => {
    if (!listItems.length) return;
    blocks.push(
      <ol key={`ol-${key}`} className="formatted-list">
        {listItems.map((item, index) => (
          <li
            key={`${key}-${index}`}
            dangerouslySetInnerHTML={{ __html: formatInline(item) }}
          />
        ))}
      </ol>,
    );
    listItems = [];
  };

  lines.forEach((line, index) => {
    const trimmed = line.trim();
    const listMatch = trimmed.match(/^\d+\.\s+(.*)$/);
    if (listMatch) {
      listItems.push(listMatch[1]);
      return;
    }

    flushList(index);

    if (!trimmed) {
      blocks.push(<div key={`gap-${index}`} className="formatted-gap" />);
      return;
    }

    blocks.push(
      <p
        key={`p-${index}`}
        dangerouslySetInnerHTML={{ __html: formatInline(trimmed) }}
      />,
    );
  });

  flushList(lines.length);

  return <>{blocks}</>;
}

function formatInline(text: string) {
  return text
    .replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>")
    .replace(/\*(.+?)\*/g, "<em>$1</em>");
}

export default function BankingApp() {
  const [mode, setMode] = useState<AuthMode>("login");
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [authError, setAuthError] = useState<string | null>(null);
  const [authLoading, setAuthLoading] = useState(false);

  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<LoginResponse["user"] | null>(null);
  const [authReady, setAuthReady] = useState(false);

  const [lookbackDays, setLookbackDays] = useState(30);
  const [activeTab, setActiveTab] = useState<Tab>("overview");
  const [snapshot, setSnapshot] = useState<BankingSnapshot | null>(null);
  const [sessions, setSessions] = useState<ChatSessionSummary[]>([]);
  const [selectedSessionId, setSelectedSessionId] = useState<number | null>(
    null,
  );
  const [sessionDetail, setSessionDetail] = useState<ChatSessionDetail | null>(
    null,
  );
  const [savedPrompts, setSavedPrompts] = useState<SavedPrompt[]>([]);
  const [refreshTick, setRefreshTick] = useState(0);

  const [chatInput, setChatInput] = useState("");
  const [chatBusy, setChatBusy] = useState(false);
  const [chatError, setChatError] = useState<string | null>(null);
  const [chatMessages, setChatMessages] = useState<ChatRow[]>([]);

  const [txnAccountId, setTxnAccountId] = useState<number | "">("");
  const [txnAmount, setTxnAmount] = useState("0");
  const [txnType, setTxnType] = useState<"Debit" | "Credit">("Debit");
  const [txnCategory, setTxnCategory] = useState("General");
  const [txnMerchant, setTxnMerchant] = useState("Bank transfer");
  const [txnDescription, setTxnDescription] = useState("Manual transaction");
  const [txnPending, setTxnPending] = useState(false);
  const [txnStatus, setTxnStatus] = useState<string | null>(null);

  const [promptTitle, setPromptTitle] = useState("");
  const [promptText, setPromptText] = useState("");
  const [promptPinned, setPromptPinned] = useState(false);
  const [promptStatus, setPromptStatus] = useState<string | null>(null);

  const apiBase = getApiBase();

  const currentSessionMessages = useMemo(() => {
    if (!sessionDetail) return [];
    return sessionDetail.messages
      .map(formatConversationMessage)
      .filter((m): m is ChatRow => m !== null);
  }, [sessionDetail]);

  const debitTransactions = useMemo(
    () =>
      (snapshot?.recentTransactions ?? []).filter(
        (item) => item.transactionType.trim().toLowerCase() === "debit",
      ),
    [snapshot],
  );

  const creditTransactions = useMemo(
    () =>
      (snapshot?.recentTransactions ?? []).filter(
        (item) => item.transactionType.trim().toLowerCase() === "credit",
      ),
    [snapshot],
  );

  const debitTotal = useMemo(
    () =>
      debitTransactions.reduce(
        (total, item) => total + Math.abs(item.amount),
        0,
      ),
    [debitTransactions],
  );

  const creditTotal = useMemo(
    () =>
      creditTransactions.reduce(
        (total, item) => total + Math.abs(item.amount),
        0,
      ),
    [creditTransactions],
  );

  const chatFeedRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    try {
      const storedAuth = window.localStorage.getItem(AUTH_STORAGE_KEY);
      if (storedAuth) {
        const parsed = JSON.parse(storedAuth) as {
          token?: string;
          user?: LoginResponse["user"];
        };
        if (parsed.token && parsed.user) {
          setToken(parsed.token);
          setUser(parsed.user);
        }
      }
    } catch {
      window.localStorage.removeItem(AUTH_STORAGE_KEY);
    } finally {
      setAuthReady(true);
    }
  }, []);

  useEffect(() => {
    if (!authReady) return;

    if (token && user) {
      window.localStorage.setItem(
        AUTH_STORAGE_KEY,
        JSON.stringify({ token, user }),
      );
      return;
    }

    window.localStorage.removeItem(AUTH_STORAGE_KEY);
  }, [authReady, token, user]);

  // Auto-scroll to bottom when messages update
  useEffect(() => {
    const feed = chatFeedRef.current;
    if (feed) {
      requestAnimationFrame(() => {
        feed.scrollTop = feed.scrollHeight;
      });
    }
  }, [chatMessages, currentSessionMessages, chatBusy]);

  useEffect(() => {
    if (!token) return;
    const authToken = token;

    let isMounted = true;

    async function loadDashboard() {
      try {
        const [overviewResult, sessionResult, promptResult] = await Promise.all(
          [
            getOverview(authToken, lookbackDays),
            getSessions(authToken),
            getSavedPrompts(authToken),
          ],
        );

        if (!isMounted) return;

        setSnapshot(overviewResult);
        setSessions(sessionResult);
        setSavedPrompts(promptResult);
        if (overviewResult.accounts.length > 0 && txnAccountId === "") {
          setTxnAccountId(overviewResult.accounts[0].accountId);
        }
      } catch (error) {
        if (!isMounted) return;
        setChatError(
          error instanceof Error ? error.message : "Failed to load dashboard",
        );
      }
    }

    void loadDashboard();

    return () => {
      isMounted = false;
    };
  }, [token, lookbackDays, refreshTick]);

  useEffect(() => {
    if (!token || !selectedSessionId) {
      setSessionDetail(null);
      return;
    }
    const authToken = token;
    const sessionId = selectedSessionId;

    let isMounted = true;
    async function loadSession() {
      try {
        const result = await getSession(authToken, sessionId);
        if (!isMounted) return;
        setSessionDetail(result);
      } catch {
        if (!isMounted) return;
        setSessionDetail(null);
      }
    }

    void loadSession();

    return () => {
      isMounted = false;
    };
  }, [token, selectedSessionId, refreshTick]);

  useEffect(() => {
    if (selectedSessionId && selectedSessionId !== sessionDetail?.sessionId) {
      const summary = sessions.find((s) => s.sessionId === selectedSessionId);
      if (summary) {
        setActiveTab("assistant");
      }
    }
  }, [selectedSessionId, sessionDetail?.sessionId, sessions]);

  const authHandler = async () => {
    setAuthError(null);
    setAuthLoading(true);
    try {
      const response =
        mode === "login"
          ? await login(email, password)
          : await register(name, email, password, confirmPassword);

      setToken(response.token);
      setUser(response.user);
      setChatMessages([]);
      setRefreshTick((value) => value + 1);
    } catch (error) {
      setAuthError(
        error instanceof Error ? error.message : "Authentication failed",
      );
    } finally {
      setAuthLoading(false);
    }
  };

  const logout = () => {
    setToken(null);
    setUser(null);
    setSnapshot(null);
    setSessions([]);
    setSessionDetail(null);
    setSavedPrompts([]);
    setChatMessages([]);
    setSelectedSessionId(null);
    setActiveTab("overview");
  };

  const startNewChat = () => {
    setActiveTab("assistant");
    setSelectedSessionId(null);
    setSessionDetail(null);
    setChatMessages([]);
    setChatInput("");
    setChatError(null);
  };

  const handleSendChat = async () => {
    if (!token || !chatInput.trim()) return;
    setChatBusy(true);
    setChatError(null);
    const outgoing = chatInput.trim();
    setChatInput("");
    setChatMessages((rows) => [...rows, { role: "user", content: outgoing }]);

    try {
      const response = await sendChat(
        token,
        outgoing,
        selectedSessionId ?? undefined,
        lookbackDays,
      );
      setChatMessages((rows) => [
        ...rows,
        {
          role: "assistant",
          content: response.assistantMessage,
          meta: `${response.modelName} • ${response.promptVersion}`,
        },
      ]);
      setSelectedSessionId(response.sessionId);
      setRefreshTick((value) => value + 1);
    } catch (error) {
      setChatError(
        error instanceof Error ? error.message : "Failed to send message",
      );
    } finally {
      setChatBusy(false);
    }
  };

  const handleCreateTransaction = async () => {
    if (!token || txnAccountId === "") return;
    setTxnStatus(null);
    try {
      const response = await createTransaction(
        token,
        Number(txnAccountId),
        Number(txnAmount),
        txnType,
        txnCategory,
        txnMerchant,
        txnDescription,
        txnPending,
      );
      setTxnStatus(
        `Saved ${txnType.toLowerCase()} for ${formatMoney(Number(txnAmount))} on ${response.account.displayName}.`,
      );
      setRefreshTick((value) => value + 1);
    } catch (error) {
      setTxnStatus(
        error instanceof Error ? error.message : "Failed to save transaction",
      );
    }
  };

  const handleSavePrompt = async () => {
    if (!token || !promptTitle.trim() || !promptText.trim()) return;
    setPromptStatus(null);
    try {
      await savePrompt(
        token,
        promptTitle.trim(),
        promptText.trim(),
        promptPinned,
      );
      setPromptTitle("");
      setPromptText("");
      setPromptPinned(false);
      setPromptStatus("Prompt saved.");
      setRefreshTick((value) => value + 1);
    } catch (error) {
      setPromptStatus(
        error instanceof Error ? error.message : "Failed to save prompt",
      );
    }
  };

  if (!authReady) {
    return (
      <main className="auth-shell">
        <section className="auth-card loading-panel">
          <p>Restoring your session...</p>
        </section>
      </main>
    );
  }

  if (!token) {
    return (
      <main className="auth-shell">
        <section className="auth-card">
          <div className="auth-brand">
            <div className="auth-logo">AI</div>
            <div>
              <p className="eyebrow">Banking AI Bot</p>
              <h1>Money intelligence, at your fingertips.</h1>
            </div>
          </div>

          <div className="auth-toggle">
            <button
              className={mode === "login" ? "active" : ""}
              onClick={() => setMode("login")}
            >
              Login
            </button>
            <button
              className={mode === "register" ? "active" : ""}
              onClick={() => setMode("register")}
            >
              Register
            </button>
          </div>

          {mode === "register" && (
            <label className="field">
              <span>Name</span>
              <input
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Your name"
              />
            </label>
          )}

          <label className="field">
            <span>Email</span>
            <input
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="you@example.com"
            />
          </label>

          <label className="field">
            <span>Password</span>
            <input
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </label>

          {mode === "register" && (
            <label className="field">
              <span>Confirm Password</span>
              <input
                type="password"
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
              />
            </label>
          )}

          {authError && <div className="toast error">{authError}</div>}

          <button
            className="primary-action"
            onClick={authHandler}
            disabled={authLoading}
          >
            {authLoading
              ? "Working..."
              : mode === "login"
                ? "Login"
                : "Create account"}
          </button>
        </section>
      </main>
    );
  }

  const accounts = snapshot?.accounts ?? [];
  const recentTransactions = snapshot?.recentTransactions ?? [];

  return (
    <main className="workspace-shell">
      <aside className="side-nav">
        <div className="brand-block">
          <div className="auth-logo">AI</div>
          <div>
            <p className="eyebrow">Signed in as</p>
            <strong style={{ textTransform: "uppercase" }}>
              {user?.name ?? user?.email}
            </strong>
          </div>
        </div>

        <div className="nav-group">
          <button
            className={
              activeTab === "overview" ? "nav-item active" : "nav-item"
            }
            onClick={() => setActiveTab("overview")}
          >
            Overview
          </button>
          <div className="assistant-nav-block">
            <div className="assistant-nav-head">
              <button
                className={
                  activeTab === "assistant" ? "nav-item active" : "nav-item"
                }
                onClick={() => setActiveTab("assistant")}
              >
                Assistant
              </button>
              <button
                className="new-chat-btn"
                title="New chat"
                aria-label="Start a new chat"
                onClick={startNewChat}
              >
                +
              </button>
            </div>

            {activeTab === "assistant" && (
              <div className="assistant-session-tree">
                <button
                  className={
                    selectedSessionId === null
                      ? "sidebar-session-item active"
                      : "sidebar-session-item"
                  }
                  onClick={startNewChat}
                >
                  <span className="sidebar-session-title">New chat</span>
                  <span className="sidebar-session-date">
                    Start a fresh conversation
                  </span>
                </button>
                {sessions.map((session) => (
                  <button
                    key={session.sessionId}
                    className={
                      selectedSessionId === session.sessionId
                        ? "sidebar-session-item active"
                        : "sidebar-session-item"
                    }
                    onClick={() => {
                      setActiveTab("assistant");
                      setSelectedSessionId(session.sessionId);
                    }}
                  >
                    <span className="sidebar-session-title">
                      {session.titleSummary}
                    </span>
                    <span className="sidebar-session-date">
                      {formatDateTime(session.startedAt)}
                    </span>
                  </button>
                ))}
              </div>
            )}
          </div>
          <button
            className={
              activeTab === "activity" ? "nav-item active" : "nav-item"
            }
            onClick={() => setActiveTab("activity")}
          >
            Activity
          </button>
        </div>

        {/* ── Sidebar sessions (ChatGPT-style) ── */}
        <button className="secondary-action" onClick={logout}>
          Logout
        </button>
      </aside>

      <section className="workspace-main">
        <header className="page-header">
          <div>
            <p className="eyebrow">Banking dashboard</p>
            <h1>Financial overview</h1>
            <p className="subtle">
              Accounts, transactions, spending insights, and AI assistant.
            </p>
          </div>

          <div className="toolbar">
            <label className="field compact">
              <span>Lookback</span>
              <select
                value={lookbackDays}
                onChange={(event) =>
                  setLookbackDays(Number(event.target.value))
                }
              >
                <option value={7}>7 days</option>
                <option value={30}>30 days</option>
                <option value={90}>90 days</option>
              </select>
            </label>
          </div>
        </header>

        {snapshot ? (
          <>
            <section className="stats-grid">
              <article className="stat-card">
                <span>Total Balance</span>
                <strong>{formatMoney(snapshot.totalBalance)}</strong>
              </article>
              <article className="stat-card">
                <span>Available</span>
                <strong>{formatMoney(snapshot.totalAvailableBalance)}</strong>
              </article>
              <article className="stat-card">
                <span>Monthly Income</span>
                <strong>{formatMoney(snapshot.monthlyIncome)}</strong>
              </article>
              <article className="stat-card">
                <span>Monthly Spend</span>
                <strong>{formatMoney(snapshot.monthlySpend)}</strong>
              </article>
            </section>

            {activeTab === "overview" && (
              <section className="content-grid">
                <article className="panel">
                  <div className="panel-header">
                    <h2>Accounts</h2>
                    <span className="subtle">{accounts.length} account(s)</span>
                  </div>
                  <div className="account-list">
                    {accounts.map((account) => (
                      <div key={account.accountId} className="account-card">
                        <div>
                          <strong>{account.displayName}</strong>
                          <p>{account.accountType}</p>
                        </div>
                        <div className="account-balances">
                          <span>{formatMoney(account.balance)}</span>
                          <small>
                            Available {formatMoney(account.availableBalance)}
                          </small>
                        </div>
                      </div>
                    ))}
                  </div>

                  <div className="transaction-form">
                    <div className="panel-header">
                      <h3>Create transaction</h3>
                      <span className="subtle">
                        Debit or credit from a selected account
                      </span>
                    </div>

                    <div className="form-grid">
                      <label className="field">
                        <span>Account</span>
                        <select
                          value={txnAccountId}
                          onChange={(event) =>
                            setTxnAccountId(
                              event.target.value
                                ? Number(event.target.value)
                                : "",
                            )
                          }
                        >
                          <option value="">Select account</option>
                          {accounts.map((account) => (
                            <option
                              key={account.accountId}
                              value={account.accountId}
                            >
                              {account.displayName}
                            </option>
                          ))}
                        </select>
                      </label>

                      <label className="field">
                        <span>Type</span>
                        <select
                          value={txnType}
                          onChange={(event) =>
                            setTxnType(event.target.value as "Debit" | "Credit")
                          }
                        >
                          <option value="Debit">Debit</option>
                          <option value="Credit">Credit</option>
                        </select>
                      </label>

                      <label className="field">
                        <span>Amount</span>
                        <input
                          type="number"
                          value={txnAmount}
                          onChange={(event) => setTxnAmount(event.target.value)}
                        />
                      </label>

                      <label className="field">
                        <span>Category</span>
                        <input
                          value={txnCategory}
                          onChange={(event) =>
                            setTxnCategory(event.target.value)
                          }
                        />
                      </label>

                      <label className="field">
                        <span>Merchant</span>
                        <input
                          value={txnMerchant}
                          onChange={(event) =>
                            setTxnMerchant(event.target.value)
                          }
                        />
                      </label>

                      <label className="field">
                        <span>Description</span>
                        <input
                          value={txnDescription}
                          onChange={(event) =>
                            setTxnDescription(event.target.value)
                          }
                        />
                      </label>
                    </div>

                    <label className="checkbox-line">
                      <input
                        type="checkbox"
                        checked={txnPending}
                        onChange={(event) =>
                          setTxnPending(event.target.checked)
                        }
                      />
                      <span>Mark as pending</span>
                    </label>

                    <div className="actions-row">
                      <button
                        className="primary-action"
                        onClick={handleCreateTransaction}
                      >
                        Save transaction
                      </button>
                      {txnStatus && <div className="toast">{txnStatus}</div>}
                    </div>
                  </div>
                </article>

                <article className="panel">
                  <div className="panel-header">
                    <h2>Spending snapshot</h2>
                    <span className="subtle">Category rollup</span>
                  </div>
                  <div className="spend-list">
                    {snapshot.categorySpend.length === 0 ? (
                      <p className="subtle">
                        No spending yet for this lookback period.
                      </p>
                    ) : (
                      snapshot.categorySpend.map((item) => (
                        <div key={item.category} className="spend-row">
                          <div>
                            <strong>{item.category}</strong>
                            <p>{item.transactionCount} transaction(s)</p>
                          </div>
                          <span>{formatMoney(item.totalAmount)}</span>
                        </div>
                      ))
                    )}
                  </div>

                  <div className="panel-header top-space">
                    <h3>Savings suggestions</h3>
                    <span className="subtle">
                      {snapshot.savingsSuggestions.length}
                    </span>
                  </div>
                  <div className="suggestion-list">
                    {snapshot.savingsSuggestions.length === 0 ? (
                      <p className="subtle">
                        No savings suggestions right now.
                      </p>
                    ) : (
                      snapshot.savingsSuggestions.map((item, index) => (
                        <div
                          key={`${item.savingsSuggestionId}-${index}`}
                          className="suggestion-card"
                        >
                          <strong>{item.title}</strong>
                          <p>{item.reason}</p>
                          <small>
                            Potential savings:{" "}
                            {formatMoney(item.estimatedMonthlySavings)}
                          </small>
                        </div>
                      ))
                    )}
                  </div>
                </article>
              </section>
            )}

            {activeTab === "activity" && (
              <section className="activity-board">
                <div className="activity-summary-grid">
                  <article className="activity-summary-card debit">
                    <span>Total debits</span>
                    <strong>{formatMoney(debitTotal)}</strong>
                    <small>{debitTransactions.length} transaction(s)</small>
                  </article>
                  <article className="activity-summary-card credit">
                    <span>Total credits</span>
                    <strong>{formatMoney(creditTotal)}</strong>
                    <small>{creditTransactions.length} transaction(s)</small>
                  </article>
                </div>

                <div className="activity-columns">
                  <article className="panel activity-panel">
                    <div className="panel-header">
                      <h2>Debits</h2>
                      <span className="subtle">Money out</span>
                    </div>
                    <div className="activity-list">
                      {debitTransactions.length === 0 ? (
                        <p className="subtle">No debit transactions in this range.</p>
                      ) : (
                        debitTransactions.map((item: Transaction) => (
                          <div key={item.transactionId} className="activity-card debit">
                            <div className="activity-card-top">
                              <div>
                                <strong>{item.merchantName}</strong>
                                <p>
                                  {item.accountDisplayName} · {item.category}
                                </p>
                              </div>
                              <div className="activity-amount debit">
                                -{formatMoney(Math.abs(item.amount))}
                              </div>
                            </div>
                            <div className="activity-card-bottom">
                              <span>{formatDateTime(item.timestamp)}</span>
                              <span className="activity-badge debit">
                                {item.isPending ? "Pending debit" : "Debit"}
                              </span>
                            </div>
                          </div>
                        ))
                      )}
                    </div>
                  </article>

                  <article className="panel activity-panel">
                    <div className="panel-header">
                      <h2>Credits</h2>
                      <span className="subtle">Money in</span>
                    </div>
                    <div className="activity-list">
                      {creditTransactions.length === 0 ? (
                        <p className="subtle">No credit transactions in this range.</p>
                      ) : (
                        creditTransactions.map((item: Transaction) => (
                          <div key={item.transactionId} className="activity-card credit">
                            <div className="activity-card-top">
                              <div>
                                <strong>{item.merchantName}</strong>
                                <p>
                                  {item.accountDisplayName} · {item.category}
                                </p>
                              </div>
                              <div className="activity-amount credit">
                                +{formatMoney(Math.abs(item.amount))}
                              </div>
                            </div>
                            <div className="activity-card-bottom">
                              <span>{formatDateTime(item.timestamp)}</span>
                              <span className="activity-badge credit">
                                {item.isPending ? "Pending credit" : "Credit"}
                              </span>
                            </div>
                          </div>
                        ))
                      )}
                    </div>
                  </article>
                </div>
              </section>
            )}

            {activeTab === "assistant" && (
              <>
                {/* ── Full-width saved prompts bar ── */}
                <section className="panel prompts-bar">
                  <div className="prompts-bar-header">
                    <h2>Saved prompts</h2>
                    <span className="subtle">{savedPrompts.length} saved</span>
                  </div>

                  <div className="prompt-list">
                    {savedPrompts.length === 0 ? (
                      <p
                        className="subtle"
                        style={{ margin: 0, fontSize: "0.88rem" }}
                      >
                        No saved prompts yet. Use the form below to add one.
                      </p>
                    ) : (
                      savedPrompts.map((prompt) => (
                        <button
                          key={prompt.savedPromptId}
                          className="prompt-card"
                          onClick={() => {
                            setChatInput(prompt.promptText);
                          }}
                        >
                          <strong>{prompt.title}</strong>
                          <p>{prompt.promptText}</p>
                        </button>
                      ))
                    )}
                  </div>

                  <div className="prompt-save-row">
                    <input
                      className="prompt-save-input"
                      value={promptTitle}
                      onChange={(event) => setPromptTitle(event.target.value)}
                      placeholder="Title"
                    />
                    <input
                      className="prompt-save-input prompt-save-input-wide"
                      value={promptText}
                      onChange={(event) => setPromptText(event.target.value)}
                      placeholder="Prompt text"
                    />
                    <label className="checkbox-line compact">
                      <input
                        type="checkbox"
                        checked={promptPinned}
                        onChange={(event) =>
                          setPromptPinned(event.target.checked)
                        }
                      />
                      <span>Pin</span>
                    </label>
                    <button
                      className="primary-action compact"
                      onClick={handleSavePrompt}
                    >
                      Save
                    </button>
                    {promptStatus && (
                      <div className="toast compact">{promptStatus}</div>
                    )}
                  </div>
                </section>

                {/* ── Chat + Sessions two-column grid ── */}
                <section className="assistant-columns">
                  <article className="panel chat-panel">
                    <div className="panel-header">
                      <h2>Assistant</h2>
                      <span className="subtle">
                        {sessionDetail
                          ? `Session #${sessionDetail.sessionId}`
                          : "New conversation"}
                      </span>
                    </div>

                    <div className="chat-feed" ref={chatFeedRef}>
                      {(() => {
                        const messages = sessionDetail
                          ? currentSessionMessages
                          : chatMessages;
                        if (messages.length === 0 && !chatBusy) {
                          return (
                            <div className="chat-empty">
                              <div className="chat-empty-icon">💬</div>
                              <h3>Start a conversation</h3>
                              <p>
                                Ask about your balances, recent transactions,
                                spending patterns, or savings tips.
                              </p>
                            </div>
                          );
                        }
                        return messages.map((message, index) => (
                          <div
                            key={`${message.role}-${index}`}
                            className={`message-row ${message.role}`}
                          >
                            <div className={`message-avatar ${message.role}`}>
                              {message.role === "user" ? "You" : "AI"}
                            </div>
                            <div className={`chat-bubble ${message.role}`}>
                              <div className="chat-meta">
                                <strong>
                                  {message.role === "user"
                                    ? "You"
                                    : "Assistant"}
                                </strong>
                                {message.meta && <span>{message.meta}</span>}
                              </div>
                              <div className="assistant-response">
                                {renderFormattedText(message.content)}
                              </div>
                            </div>
                          </div>
                        ));
                      })()}
                      {chatBusy && (
                        <div className="message-row assistant">
                          <div className="message-avatar assistant">AI</div>
                          <div className="chat-bubble assistant">
                            <div className="chat-meta">
                              <strong>Assistant</strong>
                              <span>thinking…</span>
                            </div>
                            <div className="typing-indicator">
                              <span />
                              <span />
                              <span />
                            </div>
                          </div>
                        </div>
                      )}
                    </div>

                    {chatError && (
                      <div className="toast error">{chatError}</div>
                    )}

                    <div className="chat-input-shell">
                      <textarea
                        rows={3}
                        placeholder="Ask about balances, transactions, or spending…"
                        value={chatInput}
                        onChange={(event) => setChatInput(event.target.value)}
                        onKeyDown={(event) => {
                          if (event.key === "Enter" && !event.shiftKey) {
                            event.preventDefault();
                            void handleSendChat();
                          }
                        }}
                      />
                      <div className="actions-row">
                        <button
                          className="primary-action"
                          onClick={handleSendChat}
                          disabled={chatBusy}
                        >
                          {chatBusy ? "Thinking…" : "Send"}
                        </button>
                        <button
                          className="secondary-action"
                          onClick={() =>
                            setChatInput("Show me my last 5 transactions")
                          }
                        >
                          Insert sample
                        </button>
                        <span className="chat-input-hint">
                          Enter to send · Shift+Enter for new line
                        </span>
                      </div>
                    </div>
                  </article>
                </section>
              </>
            )}
          </>
        ) : (
          <section className="panel loading-panel">
            <p>Loading banking snapshot...</p>
          </section>
        )}
      </section>
    </main>
  );
}

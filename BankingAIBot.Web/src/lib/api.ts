import type {
  AccountMutationResponse,
  AccountType,
  BankingSnapshot,
  ChatResponse,
  ChatSessionDetail,
  ChatSessionSummary,
  LoginResponse,
  SavedPrompt,
} from "./types";

const API_BASE =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7161";

export function getApiBase() {
  return API_BASE;
}

async function readError(response: Response) {
  const text = await response.text();
  return text || response.statusText;
}

async function request<T>(
  path: string,
  init: RequestInit = {},
  token?: string,
): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init.headers ?? {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(await readError(response));
  }

  return (await response.json()) as T;
}

export async function login(email: string, password: string) {
  return request<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export async function register(
  name: string,
  email: string,
  password: string,
  confirmPassword: string,
) {
  return request<LoginResponse>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify({ name, email, password, confirmPassword }),
  });
}

export async function getOverview(token: string, lookbackDays = 30) {
  return request<BankingSnapshot>(
    `/api/banking/overview?lookbackDays=${lookbackDays}`,
    {},
    token,
  );
}

export async function getAccountTypes(token: string) {
  return request<AccountType[]>("/api/banking/account-types", {}, token);
}

export async function getTransactions(token: string, lookbackDays = 30) {
  return request<{ recentTransactions: BankingSnapshot["recentTransactions"] }>(
    `/api/banking/transactions?lookbackDays=${lookbackDays}`,
    {},
    token,
  );
}

export async function getSessions(token: string) {
  return request<ChatSessionSummary[]>("/api/assistant/sessions", {}, token);
}

export async function getSession(token: string, sessionId: number) {
  return request<ChatSessionDetail>(
    `/api/assistant/sessions/${sessionId}`,
    {},
    token,
  );
}

export async function sendChat(
  token: string,
  message: string,
  sessionId?: number,
  lookbackDays = 30,
) {
  return request<ChatResponse>(
    "/api/assistant/chat",
    {
      method: "POST",
      body: JSON.stringify({
        message,
        sessionId: sessionId ?? null,
        lookbackDays,
      }),
    },
    token,
  );
}

export async function depositMoney(
  token: string,
  accountId: number,
  amount: number,
  description: string,
  merchantName?: string,
) {
  return request<AccountMutationResponse>(
    `/api/banking/accounts/${accountId}/deposit`,
    {
      method: "POST",
      body: JSON.stringify({
        amount,
        description,
        merchantName: merchantName ?? null,
      }),
    },
    token,
  );
}

export async function createTransaction(
  token: string,
  accountId: number,
  amount: number,
  transactionType: "Debit" | "Credit",
  category: string,
  merchantName: string,
  description: string,
  isPending = false,
) {
  return request<AccountMutationResponse>(
    `/api/banking/accounts/${accountId}/transactions`,
    {
      method: "POST",
      body: JSON.stringify({
        amount,
        transactionType,
        category,
        merchantName,
        description,
        isPending,
      }),
    },
    token,
  );
}

export async function getSavedPrompts(token: string) {
  return request<SavedPrompt[]>("/api/assistant/prompts", {}, token);
}

export async function savePrompt(
  token: string,
  title: string,
  promptText: string,
  isPinned = false,
) {
  return request<SavedPrompt>(
    "/api/assistant/prompts",
    {
      method: "POST",
      body: JSON.stringify({ title, promptText, isPinned }),
    },
    token,
  );
}

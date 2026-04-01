export type LoginResponse = {
  token: string;
  expiration: string;
  user: {
    userId: number;
    name: string;
    email: string;
  };
};

export type AccountType = {
  id: number;
  name: string;
};

export type Account = {
  accountId: number;
  accountType: string;
  displayName: string;
  accountStatus: string;
  balance: number;
  availableBalance: number;
  currency: string;
  isActive: boolean;
};

export type Transaction = {
  transactionId: number;
  accountId: number;
  accountDisplayName: string;
  amount: number;
  transactionType: string;
  category: string;
  description: string;
  timestamp: string;
  merchantName: string;
  isPending: boolean;
  balanceAfter?: number | null;
};

export type CategorySpend = {
  category: string;
  totalAmount: number;
  transactionCount: number;
};

export type SavingsSuggestion = {
  savingsSuggestionId: number;
  title: string;
  reason: string;
  estimatedMonthlySavings: number;
  priority: string;
  status: string;
  createdAt: string;
};

export type BankingSnapshot = {
  totalBalance: number;
  totalAvailableBalance: number;
  monthlyIncome: number;
  monthlySpend: number;
  netCashFlow: number;
  accounts: Account[];
  recentTransactions: Transaction[];
  categorySpend: CategorySpend[];
  savingsSuggestions: SavingsSuggestion[];
};

export type ChatResponse = {
  sessionId: number;
  assistantMessage: string;
  modelName: string;
  promptVersion: string;
  snapshot: BankingSnapshot;
  savingsSuggestions: SavingsSuggestion[];
};

export type ChatSessionSummary = {
  sessionId: number;
  titleSummary: string;
  startedAt: string;
  lastMessageAt?: string | null;
  status: string;
  modelName?: string | null;
};

export type ChatMessage = {
  messageId: number;
  role: string;
  content: string;
  timestamp: string;
  toolName?: string | null;
  toolCallId?: string | null;
  modelName?: string | null;
  promptVersion?: string | null;
};

export type ChatSessionDetail = {
  sessionId: number;
  titleSummary: string;
  startedAt: string;
  lastMessageAt?: string | null;
  status: string;
  modelName?: string | null;
  messages: ChatMessage[];
};

export type AccountMutationResponse = {
  account: Account;
  transaction: Transaction;
};

export type SavedPrompt = {
  savedPromptId: number;
  title: string;
  promptText: string;
  usageCount: number;
  isPinned: boolean;
  createdAt: string;
  updatedAt: string;
};

import type {
  Customer,
  CustomerDetail,
  CustomerNote,
  Account,
  Transaction,
  StaffSession,
  ScheduledPayment,
} from '../types';

const BASE = '/api/crm';

function getToken(): string | null {
  const session = sessionStorage.getItem('staff_session');
  if (!session) return null;
  return (JSON.parse(session) as StaffSession).token;
}

async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    ...(init?.headers as Record<string, string>),
  };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (init?.body) headers['Content-Type'] = 'application/json';

  const res = await fetch(url, { ...init, headers });
  if (res.status === 401) {
    sessionStorage.removeItem('staff_session');
    window.location.href = '/';
    throw new Error('Unauthorized');
  }
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(text || `Request failed: ${res.status}`);
  }
  return res.json() as Promise<T>;
}

// Auth
export async function login(
  username: string,
  password: string,
): Promise<StaffSession> {
  const res = await fetch(`${BASE}/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  });
  if (!res.ok) throw new Error('Invalid credentials');
  return res.json() as Promise<StaffSession>;
}

// Customers
export function getCustomers(
  search?: string,
  page = 1,
  pageSize = 20,
): Promise<{ customers: Customer[]; total: number; page: number; pageSize: number }> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (search) params.set('search', search);
  return fetchJson(`${BASE}/customers?${params}`);
}

export function getCustomer(id: number): Promise<CustomerDetail> {
  return fetchJson(`${BASE}/customers/${id}`);
}

export function updateCustomer(
  id: number,
  data: {
    firstName: string;
    lastName: string;
    email: string;
    phone: string | null;
    dateOfBirth: string;
  },
): Promise<CustomerDetail> {
  return fetchJson(`${BASE}/customers/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

// Accounts
export function getAccount(id: number): Promise<Account> {
  return fetchJson(`${BASE}/accounts/${id}`);
}

export function adjustBalance(
  id: number,
  amount: number,
  reason: string,
): Promise<{ transaction: Transaction; newBalance: number }> {
  return fetchJson(`${BASE}/accounts/${id}/adjust`, {
    method: 'POST',
    body: JSON.stringify({ amount, reason }),
  });
}

export function closeAccount(
  id: number,
  force = false,
): Promise<{ message: string }> {
  return fetchJson(`${BASE}/accounts/${id}/close`, {
    method: 'POST',
    body: JSON.stringify({ force }),
  });
}

// Transactions
export function getTransactions(
  accountId: number,
  params?: {
    type?: string;
    from?: string;
    to?: string;
    minAmount?: number;
    maxAmount?: number;
    page?: number;
    pageSize?: number;
  },
): Promise<{ transactions: Transaction[]; total: number; page: number; pageSize: number }> {
  const sp = new URLSearchParams();
  if (params?.type) sp.set('type', params.type);
  if (params?.from) sp.set('from', params.from);
  if (params?.to) sp.set('to', params.to);
  if (params?.minAmount !== undefined) sp.set('minAmount', String(params.minAmount));
  if (params?.maxAmount !== undefined) sp.set('maxAmount', String(params.maxAmount));
  sp.set('page', String(params?.page ?? 1));
  sp.set('pageSize', String(params?.pageSize ?? 20));
  return fetchJson(`${BASE}/accounts/${accountId}/transactions?${sp}`);
}

// Notes
export function getNotes(customerId: number): Promise<CustomerNote[]> {
  return fetchJson(`${BASE}/customers/${customerId}/notes`);
}

export function addNote(
  customerId: number,
  content: string,
): Promise<CustomerNote> {
  return fetchJson(`${BASE}/customers/${customerId}/notes`, {
    method: 'POST',
    body: JSON.stringify({ content }),
  });
}

// Scheduled Payments
export function getScheduledPayments(accountId: number): Promise<ScheduledPayment[]> {
  return fetchJson(`${BASE}/accounts/${accountId}/scheduled-payments`);
}

import type { Account, Customer, Transaction, ScheduledPayment } from '../types';

const BASE = '/api';

async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, init);
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(text || `Request failed: ${res.status}`);
  }
  return res.json() as Promise<T>;
}

export interface CustomerPage {
  customers: Customer[];
  total: number;
  page: number;
  pageSize: number;
}

export function getCustomers(search?: string, page = 1, pageSize = 20, signal?: AbortSignal): Promise<CustomerPage> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (search) params.set('search', search);
  return fetchJson<CustomerPage>(`${BASE}/customers?${params}`, signal ? { signal } : undefined);
}

export function getCustomerAccounts(customerId: string): Promise<Account[]> {
  return fetchJson<Account[]>(`${BASE}/customers/${customerId}/accounts`);
}

export function getAccount(accountId: string): Promise<Account> {
  return fetchJson<Account>(`${BASE}/accounts/${accountId}`);
}

export function getTransactions(
  accountId: string,
  page = 1,
  pageSize = 20,
): Promise<Transaction[]> {
  return fetchJson<Transaction[]>(
    `${BASE}/accounts/${accountId}/transactions?page=${page}&pageSize=${pageSize}`,
  );
}

export function deposit(accountId: string, amount: number, description: string) {
  return fetchJson<void>(`${BASE}/accounts/${accountId}/deposit`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ amount, description }),
  });
}

export function withdraw(accountId: string, amount: number, description: string) {
  return fetchJson<void>(`${BASE}/accounts/${accountId}/withdraw`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ amount, description }),
  });
}

export function transfer(
  fromAccountId: string,
  toAccountId: string,
  amount: number,
  description: string,
) {
  return fetchJson<void>(`${BASE}/transfers`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ fromAccountId, toAccountId, amount, description }),
  });
}

export function lookupAccount(bsb: string, accountNumber: string) {
  return fetchJson<import('../types').PayeeLookup>(
    `${BASE}/payments/lookup?bsb=${encodeURIComponent(bsb)}&accountNumber=${encodeURIComponent(accountNumber)}`,
  );
}

export function pay(
  callerCustomerId: string,
  fromAccountId: string,
  toBsb: string,
  toAccountNumber: string,
  amount: number,
  description: string,
  reference: string,
) {
  return fetchJson<void>(`${BASE}/payments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ callerCustomerId: Number(callerCustomerId), fromAccountId: Number(fromAccountId), toBsb, toAccountNumber, amount, description, reference }),
  });
}

export function getScheduledPayments(accountId: string): Promise<ScheduledPayment[]> {
  return fetchJson<ScheduledPayment[]>(`${BASE}/accounts/${accountId}/scheduled-payments`);
}

export function createScheduledPayment(data: {
  accountId: number;
  payeeName: string;
  payeeBsb?: string;
  payeeAccountNumber?: string;
  payeeAccountId?: number;
  amount: number;
  description?: string;
  reference?: string;
  frequency: string;
  startDate: string;
  endDate?: string;
}) {
  return fetchJson<ScheduledPayment>(`${BASE}/scheduled-payments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });
}

export function cancelScheduledPayment(id: number) {
  return fetchJson<{ message: string }>(`${BASE}/scheduled-payments/${id}`, {
    method: 'DELETE',
  });
}

import type { Account, Customer, Transaction } from '../types';

const BASE = '/api';

async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, init);
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(text || `Request failed: ${res.status}`);
  }
  return res.json() as Promise<T>;
}

export function getCustomers(): Promise<Customer[]> {
  return fetchJson<Customer[]>(`${BASE}/customers`);
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

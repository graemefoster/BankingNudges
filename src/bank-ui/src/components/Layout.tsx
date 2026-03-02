import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useState, useEffect } from 'react';

const HomeIcon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
    <path d="M3 9.5L12 3l9 6.5V20a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V9.5z" />
    <path d="M9 21V12h6v9" />
  </svg>
);

const TransferIcon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
    <path d="M7 16l-4-4 4-4" />
    <path d="M3 12h14" />
    <path d="M17 8l4 4-4 4" />
    <path d="M21 12H7" />
  </svg>
);

const PayIcon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
    <rect x="2" y="5" width="20" height="14" rx="2" />
    <path d="M2 10h20" />
  </svg>
);

const navItems = [
  { to: '/dashboard', label: 'Home', icon: HomeIcon },
  { to: '/transfer', label: 'Transfer', icon: TransferIcon },
  { to: '/pay', label: 'Pay', icon: PayIcon },
];

export default function Layout() {
  const location = useLocation();
  const isLogin = location.pathname === '/';
  const [virtualDate, setVirtualDate] = useState<string | null>(null);
  const [daysAdvanced, setDaysAdvanced] = useState(0);

  useEffect(() => {
    fetch('/api/time-travel/current')
      .then((r) => r.json())
      .then((data) => {
        setVirtualDate(data.virtualToday);
        setDaysAdvanced(data.daysAdvanced);
      })
      .catch(() => {});
  }, [location]);

  return (
    <div className="min-h-screen bg-dark-bg flex flex-col">
      {/* Virtual time banner */}
      {daysAdvanced > 0 && virtualDate && (
        <div className="bg-amber-500 text-white text-center text-xs py-1 font-medium">
          ⏰ Virtual Date: {virtualDate} ({daysAdvanced} days advanced)
        </div>
      )}

      {/* Header */}
      <header className="bg-dark-surface border-b border-border">
        <div className="h-0.5 bg-gradient-to-r from-accent-teal to-accent-cyan" />
        <div className="max-w-md mx-auto flex items-center justify-between px-4 py-3">
          <h1 className="text-lg font-bold text-text-primary tracking-tight">
            Bank of Graeme
          </h1>
          {!isLogin && (
            <NavLink
              to="/"
              className="text-xs text-text-secondary hover:text-accent-teal transition-colors"
            >
              Switch User
            </NavLink>
          )}
        </div>
      </header>

      {/* Main content */}
      <main className="flex-1 overflow-y-auto pb-20">
        <div className="max-w-md mx-auto px-4 py-4">
          <Outlet />
        </div>
      </main>

      {/* Bottom nav */}
      {!isLogin && (
        <nav className="fixed bottom-0 inset-x-0 bg-dark-surface/80 backdrop-blur-xl border-t border-border">
          <div className="max-w-md mx-auto flex">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  `flex-1 flex flex-col items-center py-3 text-xs transition-colors ${
                    isActive
                      ? 'text-accent-teal font-semibold'
                      : 'text-text-muted hover:text-text-secondary'
                  }`
                }
              >
                <item.icon />
                <span className="mt-1">{item.label}</span>
              </NavLink>
            ))}
          </div>
        </nav>
      )}
    </div>
  );
}

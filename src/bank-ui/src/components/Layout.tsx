import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useState, useEffect } from 'react';

const navItems = [
  { to: '/dashboard', label: 'Home', icon: '🏠' },
  { to: '/transfer', label: 'Transfer', icon: '💸' },
  { to: '/pay', label: 'Pay', icon: '💳' },
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
    <div className="min-h-screen bg-light-bg flex flex-col">
      {/* Virtual time banner */}
      {daysAdvanced > 0 && virtualDate && (
        <div className="bg-amber-500 text-white text-center text-xs py-1 font-medium">
          ⏰ Virtual Date: {virtualDate} ({daysAdvanced} days advanced)
        </div>
      )}

      {/* Header */}
      <header className="bg-brand-purple px-4 py-3 shadow-md">
        <div className="max-w-md mx-auto flex items-center justify-between">
          <h1 className="text-lg font-bold text-white">
            🏦 Bank of Graeme
          </h1>
          {!isLogin && (
            <NavLink
              to="/"
              className="text-xs text-white/70 hover:text-white transition-colors"
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
        <nav className="fixed bottom-0 inset-x-0 bg-white border-t border-gray-200 shadow-lg">
          <div className="max-w-md mx-auto flex">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  `flex-1 flex flex-col items-center py-3 text-xs transition-colors ${
                    isActive
                      ? 'text-brand-purple font-semibold'
                      : 'text-text-secondary hover:text-brand-purple'
                  }`
                }
              >
                <span className="text-xl mb-0.5">{item.icon}</span>
                {item.label}
              </NavLink>
            ))}
          </div>
        </nav>
      )}
    </div>
  );
}

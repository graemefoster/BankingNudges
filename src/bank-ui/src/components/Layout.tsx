import { NavLink, Outlet, useLocation } from 'react-router-dom';

const navItems = [
  { to: '/dashboard', label: 'Home', icon: '🏠' },
  { to: '/transfer', label: 'Transfer', icon: '💸' },
];

export default function Layout() {
  const location = useLocation();
  const isLogin = location.pathname === '/';

  return (
    <div className="min-h-screen bg-dark-bg flex flex-col">
      {/* Header */}
      <header className="bg-dark-card border-b border-white/10 px-4 py-3">
        <div className="max-w-md mx-auto flex items-center justify-between">
          <h1 className="text-lg font-bold text-brand-purple">
            🏦 Bank of Graeme
          </h1>
          {!isLogin && (
            <NavLink
              to="/"
              className="text-xs text-gray-400 hover:text-white transition-colors"
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
        <nav className="fixed bottom-0 inset-x-0 bg-dark-card border-t border-white/10">
          <div className="max-w-md mx-auto flex">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  `flex-1 flex flex-col items-center py-3 text-xs transition-colors ${
                    isActive
                      ? 'text-brand-purple'
                      : 'text-gray-400 hover:text-white'
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

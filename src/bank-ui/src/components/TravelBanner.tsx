import type { ActiveHoliday } from '../types';

interface DestinationInfo {
  flag: string;
  greeting: string;
  greetingLanguage: string;
}

const destinationMap: Record<string, DestinationInfo> = {
  bali: { flag: '🇮🇩', greeting: 'Selamat datang di Indonesia!', greetingLanguage: 'Indonesian' },
  indonesia: { flag: '🇮🇩', greeting: 'Selamat datang di Indonesia!', greetingLanguage: 'Indonesian' },
  thailand: { flag: '🇹🇭', greeting: 'ยินดีต้อนรับสู่ประเทศไทย!', greetingLanguage: 'Thai' },
  fiji: { flag: '🇫🇯', greeting: 'Bula! Welcome to Fiji!', greetingLanguage: 'Fijian' },
  japan: { flag: '🇯🇵', greeting: 'ようこそ日本へ！', greetingLanguage: 'Japanese' },
  'new zealand': { flag: '🇳🇿', greeting: 'Kia ora! Welcome to New Zealand!', greetingLanguage: 'Māori' },
  europe: { flag: '🇪🇺', greeting: 'Bienvenue en Europe!', greetingLanguage: 'French' },
  france: { flag: '🇫🇷', greeting: 'Bienvenue en France!', greetingLanguage: 'French' },
  germany: { flag: '🇩🇪', greeting: 'Willkommen in Deutschland!', greetingLanguage: 'German' },
  italy: { flag: '🇮🇹', greeting: 'Benvenuto in Italia!', greetingLanguage: 'Italian' },
  spain: { flag: '🇪🇸', greeting: '¡Bienvenido a España!', greetingLanguage: 'Spanish' },
  usa: { flag: '🇺🇸', greeting: 'Welcome to the USA!', greetingLanguage: 'English' },
  'united states': { flag: '🇺🇸', greeting: 'Welcome to the USA!', greetingLanguage: 'English' },
  uk: { flag: '🇬🇧', greeting: 'Welcome to the United Kingdom!', greetingLanguage: 'English' },
  'united kingdom': { flag: '🇬🇧', greeting: 'Welcome to the United Kingdom!', greetingLanguage: 'English' },
  singapore: { flag: '🇸🇬', greeting: 'Welcome to Singapore!', greetingLanguage: 'English' },
  vietnam: { flag: '🇻🇳', greeting: 'Chào mừng đến Việt Nam!', greetingLanguage: 'Vietnamese' },
  korea: { flag: '🇰🇷', greeting: '한국에 오신 것을 환영합니다!', greetingLanguage: 'Korean' },
};

function getDestinationInfo(destination: string): DestinationInfo {
  const key = destination.toLowerCase();
  return destinationMap[key] ?? { flag: '✈️', greeting: `Welcome to ${destination}!`, greetingLanguage: 'English' };
}

function daysRemaining(endDate: string): number {
  const end = new Date(endDate);
  const now = new Date();
  return Math.max(0, Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24)));
}

interface Props {
  holiday: ActiveHoliday;
}

export default function TravelBanner({ holiday }: Props) {
  const info = getDestinationInfo(holiday.destination);
  const remaining = daysRemaining(holiday.endDate);

  return (
    <div className="relative overflow-hidden rounded-2xl mb-6 bg-gradient-to-r from-blue-900/80 via-indigo-900/70 to-purple-900/60 border border-white/10">
      {/* Subtle pattern overlay */}
      <div className="absolute inset-0 opacity-10">
        <div className="absolute top-1 right-4 text-6xl">{info.flag}</div>
      </div>

      <div className="relative px-5 py-4">
        <div className="flex items-start gap-3">
          <span className="text-3xl mt-0.5">{info.flag}</span>
          <div className="flex-1 min-w-0">
            <p className="text-white font-semibold text-base leading-tight">
              {info.greeting}
            </p>
            <p className="text-white/60 text-xs mt-1">
              {remaining > 0
                ? `${remaining} day${remaining !== 1 ? 's' : ''} remaining · Enjoy your trip!`
                : 'Last day — safe travels home! 🏠'}
            </p>
          </div>
          <span className="text-[10px] text-white/40 shrink-0 mt-1">
            ✈️ {holiday.destination}
          </span>
        </div>
      </div>
    </div>
  );
}

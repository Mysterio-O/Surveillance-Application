import type { AppCategory } from '../../types/activity';

const CATEGORY_STYLES: Record<AppCategory, string> = {
  coding:         'bg-green/20 text-green',
  browser_work:   'bg-blue/20 text-blue',
  browser:        'bg-sky/20 text-sky',
  communication:  'bg-mauve/20 text-mauve',
  docs:           'bg-yellow/20 text-yellow',
  terminal:       'bg-teal/20 text-teal',
  media:          'bg-red/20 text-red',
  system:         'bg-subtext0/20 text-subtext0',
  idle:           'bg-surface0/50 text-subtext0',
  other:          'bg-surface1/50 text-subtext0',
};

const CATEGORY_LABELS: Record<AppCategory, string> = {
  coding:         '💻 Coding',
  browser_work:   '🌐 Work',
  browser:        '🌐 Browser',
  communication:  '💬 Chat',
  docs:           '📄 Docs',
  terminal:       '⬛ Terminal',
  media:          '🎵 Media',
  system:         '⚙️ System',
  idle:           '💤 Idle',
  other:          '🔷 Other',
};

export function CategoryBadge({ category }: { category: AppCategory }) {
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${CATEGORY_STYLES[category] ?? 'bg-surface1 text-subtext0'}`}>
      {CATEGORY_LABELS[category] ?? category}
    </span>
  );
}

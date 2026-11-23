import { gettingStarted } from './guides/getting-started.mjs';
import { foundations } from './guides/foundations.mjs';
import { markup } from './guides/markup.mjs';
import { extending } from './guides/extending.mjs';
export function guides(h) {
  return [...gettingStarted(h), ...foundations(h), ...markup(h), ...extending(h)];
}

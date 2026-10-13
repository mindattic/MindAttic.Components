// Playwright configuration for the MindAttic.Components web/ package + the sites that consume it.
//
//   TEST_MODE=local (default)  sites are served from their MindAttic.Web folders by lib/static-server.mjs, and every
//                              https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@<tag>/web/** request is answered
//                              from THIS working tree (so tests pass before a tag is published, and fail loudly
//                              when a referenced file does not exist locally). No other network is allowed.
//   TEST_MODE=live             real sites, real CDN, no interception (external link navigations are still
//                              stubbed so a click never leaves the page under test).
//
//   PW_CHANNEL                 'chrome' (default, uses the installed Google Chrome), 'msedge', or 'chromium'
//                              (Playwright's own browser: run `npx playwright install chromium` first).
import { defineConfig } from '@playwright/test';

const live = process.env.TEST_MODE === 'live';
const ch = process.env.PW_CHANNEL || 'chrome';
const channel = ch === 'chromium' ? undefined : ch;

export default defineConfig({
  testDir: './specs',
  // Per-run artefacts (traces, screenshots). Override with PW_OUTPUT_DIR when two runs happen at once so they do
  // not delete each other's files (Playwright empties this folder at the start of every run).
  outputDir: process.env.PW_OUTPUT_DIR || './test-results',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: true,
  workers: process.env.CI ? 2 : undefined,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : [['list']],
  use: {
    channel,
    headless: true,
    // Headless Chrome hides scrollbars by default, which gives every page the full viewport width and hides
    // real overflow: on a desktop browser the vertical scrollbar takes ~15-17px, so a page that fits 320px
    // headless can still scroll sideways for a visitor. Keep classic scrollbars so layout checks see that.
    launchOptions: { ignoreDefaultArgs: ['--hide-scrollbars'] },
    trace: 'retain-on-failure',
  },
  webServer: live
    ? undefined
    : {
        command: 'node lib/static-server.mjs',
        url: 'http://127.0.0.1:4173/__health',
        reuseExistingServer: true,
        timeout: 20_000,
      },
  projects: [
    { name: 'assets', testDir: './specs/assets' },
    { name: 'cdn', testDir: './specs/cdn' },
    { name: 'sites', testDir: './specs/sites' },
    { name: 'components', testDir: './specs/components' },
  ],
});

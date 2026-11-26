/*
 * Runs before the stylesheet paints, so a stored dark choice never flashes the
 * light palette on a cold load. Same-origin external script because the CSP
 * (script-src 'self') forbids inline scripts. The app repeats this resolution
 * in src/main.tsx for tests and non-HTML entry points.
 */
;(function () {
  try {
    var stored = window.localStorage.getItem('crudworks-color-mode')
    var mode =
      stored === 'light' || stored === 'dark'
        ? stored
        : window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
          ? 'dark'
          : 'light'

    document.documentElement.setAttribute('data-color-mode', mode)
  } catch {
    document.documentElement.setAttribute('data-color-mode', 'light')
  }
})()

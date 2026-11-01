const form = document.querySelector('[data-search-form]');
const input = document.querySelector('[data-search-input]');
const title = document.querySelector('[data-results-title]');
const list = document.querySelector('[data-results-list]');

const isResultsPage = Boolean(title && list);

// Anchor every asset/link to this script's own location (always cybersearch.com/app.js),
// so the site works under any hosting prefix (file:// in the editor, the blob content
// host, or the same-origin /cq-web/ host) and from both cybersearch.com/ and
// cybersearch.com/results/ without depending on a fixed origin-absolute path.
const scriptUrl = (document.currentScript && document.currentScript.src) || location.href;
const siteRoot = new URL('.', scriptUrl).href;   // .../cybersearch.com/
const webRoot = new URL('../', siteRoot).href;   // .../web/  (authored result links are web-root-relative)

// Resolve a "/site/page" web-root-relative link (as stored in search_config.json) to a
// concrete URL under the current host. Absolute URLs with a scheme are left untouched.
function resolveLink(target) {
  if (!target) return target;
  if (/^[a-z][a-z0-9+.-]*:/i.test(target)) return target;
  if (target.charAt(0) === '/') return new URL(target.slice(1), webRoot).href;
  return new URL(target, siteRoot).href;
}

function readQuery() {
  return new URLSearchParams(location.search).get('q') || '';
}

function escapeText(value) {
  return String(value).replace(/[&<>"']/g, (character) => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
  })[character]);
}

function levenshteinDistance(a, b) {
  if (a === b) return 0;
  if (!a) return b.length;
  if (!b) return a.length;

  let previous = Array.from({ length: b.length + 1 }, (_, index) => index);
  let current = Array(b.length + 1).fill(0);

  for (let i = 0; i < a.length; i += 1) {
    current[0] = i + 1;
    for (let j = 0; j < b.length; j += 1) {
      const cost = a[i] === b[j] ? 0 : 1;
      current[j + 1] = Math.min(
        previous[j + 1] + 1,
        current[j] + 1,
        previous[j] + cost
      );
    }
    [previous, current] = [current, previous];
  }

  return previous[b.length];
}

function score(query, candidate) {
  if (candidate === query) return 1;
  if (candidate.includes(query) || query.includes(candidate)) return 0.95;
  const longest = Math.max(query.length, candidate.length);
  if (!longest) return 0;
  const similarity = 1 - levenshteinDistance(query, candidate) / longest;
  return similarity >= 0.3 ? similarity : 0;
}

function findBestMatch(groups, query) {
  const trimmed = query.trim().toLowerCase();
  if (!trimmed) return null;

  let best = null;
  for (const group of groups) {
    for (const candidate of group.queries) {
      const currentScore = score(trimmed, candidate.toLowerCase());
      if (currentScore > 0 && (!best || currentScore > best.score)) {
        best = { group, matchedQuery: candidate, score: currentScore };
      }
    }
  }
  return best;
}

function resultCard(result) {
  const tag = result.interactive ? 'a' : 'article';
  const href = result.interactive ? ` href="${escapeText(resolveLink(result.realUrl || result.url))}"` : '';
  const label = result.interactive
    ? `<span>${escapeText(result.url)}</span>`
    : '<span class="unavailable">not publicly available</span>';

  return `<${tag} class="result-card"${href}>
    <h2>${escapeText(result.title)}</h2>
    <p>${escapeText(result.description)}</p>
    ${label}
  </${tag}>`;
}

async function renderResults() {
  const query = readQuery().trim();
  input.value = query;

  if (!query) {
    title.textContent = 'Type something to begin your search.';
    list.innerHTML = '';
    return;
  }

  const response = await fetch(`${siteRoot}assets/search_config.json`);
  const groups = await response.json();
  const match = findBestMatch(groups, query);

  if (!match || !match.group.results.length) {
    title.textContent = `No curated results found for "${query}".`;
    list.innerHTML = '';
    return;
  }

  title.textContent = `Showing results for "${query}"`;
  list.innerHTML = match.group.results.map(resultCard).join('');
}

form.addEventListener('submit', (event) => {
  event.preventDefault();
  const query = input.value.trim();
  // Target the concrete index.html: the content host serves exact paths only and
  // does not resolve a directory URL ("results/") to its index.html.
  const target = query
    ? `${siteRoot}results/index.html?q=${encodeURIComponent(query)}`
    : `${siteRoot}index.html`;
  location.href = target;
});

if (isResultsPage) {
  renderResults().catch(() => {
    title.textContent = 'Failed to load search results.';
    list.innerHTML = '';
  });
} else {
  input.value = readQuery();
  input.focus();
}

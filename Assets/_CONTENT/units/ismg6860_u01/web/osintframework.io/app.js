// D3 CDN build exposes a global `d3`.
const d3 = globalThis.d3;

// Robust path: resolve mindmap.json relative to this script location,
// so the whole site can be placed in any folder.
const SCRIPT_URL = (() => {
  try {
    return new URL(document.currentScript?.src || globalThis.location?.href);
  } catch {
    return new URL(globalThis.location?.href);
  }
})();
const JSON_URL = new URL('mindmap.json', SCRIPT_URL);

const appEl = document.getElementById('app');
const statusEl = document.getElementById('status');

function setStatus(message) {
  if (!message) {
    statusEl.textContent = '';
    statusEl.classList.remove('is-visible');
    return;
  }
  statusEl.textContent = message;
  statusEl.classList.add('is-visible');
}

function setStatusHtml(html) {
  if (!html) {
    statusEl.innerHTML = '';
    statusEl.classList.remove('is-visible');
    return;
  }
  statusEl.innerHTML = html;
  statusEl.classList.add('is-visible');
}

function collapseAll(root) {
  root.each((d) => {
    // Collapse everything except the root level, so the initial view is fully folded.
    if (d.depth >= 1 && d.children) {
      d._children = d.children;
      d.children = null;
    }
  });
}

function escapeHtml(value) {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function radialPoint(x, y) {
  // x: angle in radians; y: radius
  return [y * Math.cos(x - Math.PI / 2), y * Math.sin(x - Math.PI / 2)];
}

async function loadData() {
  // Preferred: bundled data (works for file:// and http(s) without any fetch/CORS).
  if (globalThis.MINDMAP_DATA && typeof globalThis.MINDMAP_DATA === 'object') {
    return globalThis.MINDMAP_DATA;
  }

  // When the page is opened via file://, browsers typically block fetch() due to CORS.
  // In that case, fall back to a local file picker (no server needed).
  if (globalThis.location?.protocol === 'file:') {
    return loadDataViaFilePicker();
  }

  const res = await fetch(JSON_URL, { cache: 'no-store' });
  if (!res.ok) throw new Error(`Failed to load ${JSON_URL} (${res.status})`);
  return res.json();
}

async function loadDataViaFilePicker() {
  if (!('showOpenFilePicker' in globalThis)) {
    throw new Error('This browser does not support the file picker. Please serve the site via an HTTP server (e.g., VS Code Live Server).');
  }

  const [handle] = await globalThis.showOpenFilePicker({
    multiple: false,
    types: [
      {
        description: 'Mind map JSON',
        accept: { 'application/json': ['.json'] }
      }
    ]
  });

  const file = await handle.getFile();
  const text = await file.text();
  return JSON.parse(text);
}

function renderMindmap(data) {
  appEl.innerHTML = '';

  const width = appEl.clientWidth;
  const height = appEl.clientHeight;
  const radius = Math.min(width, height) / 2 - 40;
  // Distance between successive depth rings (layers) without changing data.
  // 1 = original spacing, higher = bigger gaps, lower = tighter layers.
  const radiusScale = 1.3;

  const root = d3.hierarchy(data);
  root.x0 = 0;
  root.y0 = 0;

  // Start with most deep nodes collapsed for readability
  collapseAll(root);

  const tree = d3.tree()
    .size([2 * Math.PI, radius])
    .separation((a, b) => (a.parent === b.parent ? 1 : 2) / a.depth);

  let pinnedNode = null;

  const svg = d3.select(appEl)
    .append('svg')
    .attr('viewBox', [-(width / 2), -(height / 2), width, height])
    .attr('role', 'img')
    .attr('aria-label', 'OSINT mind map');

  const g = svg.append('g');

  const rp = (x, y) => radialPoint(x, y * radiusScale);

  const zoom = d3.zoom()
    .scaleExtent([0.35, 3.5])
    .on('zoom', (event) => {
      g.attr('transform', event.transform);
    });

  svg.call(zoom);

  function nodeKind(d) {
    const t = (d.data && d.data.type) ? String(d.data.type) : '';
    if (t) return t;
    if (d.depth === 0) return 'root';
    if (d.children || d._children) return 'category';
    return 'tool';
  }

  function labelFor(d) {
    return d.data?.name ?? '';
  }

  function showNodeInfo(d) {
    const name = escapeHtml(d.data?.name);
    const desc = escapeHtml(d.data?.description);
    const url = escapeHtml(d.data?.url);

    const parts = [];
    parts.push(`<div><strong>${name}</strong></div>`);
    if (desc) parts.push(`<div style="margin-top:6px;">${desc}</div>`);
    if (url) parts.push(`<div style="margin-top:6px;">URL: <code>${url}</code></div>`);

    setStatusHtml(parts.join(''));
  }

  function onNodeClick(event, d) {
    event?.stopPropagation?.();

    const url = d.data?.url;
    if (url) {
      // In training/simulation mode we show info about the target, we don't navigate to it.
      pinnedNode = d;
      showNodeInfo(d);
      return;
    }

    if (d.children) {
      d._children = d.children;
      d.children = null;
    } else if (d._children) {
      d.children = d._children;
      d._children = null;
    }
    update(d);
  }

  function describe(d) {
    const parts = [];
    if (d.data?.name) parts.push(d.data.name);
    if (d.data?.description) parts.push(d.data.description);
    if (d.data?.url) parts.push(`URL: ${d.data.url}`);
    return parts.join('; ');
  }

  // Compute initial layout, then collapse everything and set starting positions.
  tree(root);
  collapseAll(root);
  tree(root);
  root.each((d) => {
    d.x0 = d.x;
    d.y0 = d.y;
  });

  function update(source = root) {
    const duration = 350;
    const t = svg.transition().duration(duration).ease(d3.easeCubicOut);

    tree(root);

    const nodes = root.descendants();
    const links = root.links();

    const [sx0, sy0] = rp(source.x0 ?? source.x, source.y0 ?? source.y);
    const [sx, sy] = rp(source.x, source.y);

    // LINKS
    const linkSel = g.selectAll('path.link')
      .data(links, (d) => d.target.data.id || `${d.source.data.name}->${d.target.data.name}`);

    linkSel.enter()
      .append('path')
      .attr('class', 'link')
      .attr('d', () => `M${sx0},${sy0}L${sx0},${sy0}`)
      .merge(linkSel)
      .transition(t)
      .attr('d', (d) => {
        const s = rp(d.source.x, d.source.y);
        const tt = rp(d.target.x, d.target.y);
        return `M${s[0]},${s[1]}L${tt[0]},${tt[1]}`;
      });

    linkSel.exit()
      .transition(t)
      .attr('d', () => `M${sx},${sy}L${sx},${sy}`)
      .remove();

    // NODES
    const nodeSel = g.selectAll('g.node')
      .data(nodes, (d) => d.data.id || d.data.name);

    const nodeEnter = nodeSel.enter()
      .append('g')
      .attr('class', 'node')
      .attr('tabindex', 0)
      .on('click', onNodeClick)
      .on('keydown', (event, d) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          onNodeClick(event, d);
        }
      })
      .on('mouseenter', (event, d) => {
        if (!pinnedNode) setStatus(describe(d));
      })
      .on('mouseleave', () => {
        if (!pinnedNode) setStatus('');
      })
      .attr('transform', () => `translate(${sx0},${sy0})`);

    nodeEnter.append('circle')
      .attr('r', (d) => {
        const kind = nodeKind(d);
        if (kind === 'root') return 7;
        if (kind === 'category') return 6;
        return 5;
      });

    nodeEnter.append('text')
      .attr('dy', '0.32em')
      .attr('x', (d) => (d.x < Math.PI ? 10 : -10))
      .attr('text-anchor', (d) => (d.x < Math.PI ? 'start' : 'end'))
      .text((d) => labelFor(d));

    const nodeMerge = nodeEnter.merge(nodeSel);

    nodeMerge
      .attr('aria-label', (d) => describe(d))
      .classed('is-category', (d) => nodeKind(d) === 'category')
      .classed('is-tool', (d) => nodeKind(d) === 'tool')
      .classed('is-link', (d) => Boolean(d.data?.url))
      .transition(t)
      .attr('transform', (d) => {
        const [x, y] = rp(d.x, d.y);
        return `translate(${x},${y})`;
      });

    nodeMerge.select('text')
      .attr('x', (d) => (d.x < Math.PI ? 10 : -10))
      .attr('text-anchor', (d) => (d.x < Math.PI ? 'start' : 'end'))
      .text((d) => labelFor(d));

    nodeSel.exit()
      .transition(t)
      .attr('transform', () => `translate(${sx},${sy})`)
      .remove();

    // Persist current positions for next transition.
    root.each((d) => {
      d.x0 = d.x;
      d.y0 = d.y;
    });
  }

  update(root);

  // Resize handling
  let resizeTimer = null;
  window.addEventListener('resize', () => {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(() => {
      renderMindmap(data);
    }, 100);
  }, { passive: true });
}

(async () => {
  try {
    if (!d3) {
      setStatus('D3 failed to load. Check your network connection / CDN access, or host d3 locally.');
      return;
    }
    setStatus('Loading mind map data…');
    const data = await loadData();
    setStatus('');
    renderMindmap(data);
  } catch (err) {
    console.error(err);

    const msg = String(err?.message || err);
    // Common gotcha: opening file:// blocks fetch
    if (msg.includes('Failed to fetch') || globalThis.location?.protocol === 'file:') {
      setStatusHtml(
        'Could not load <code>mindmap.json</code> due to browser security (CORS). '
        + '<button id="pick-json" style="margin-left:8px; padding:6px 10px; border-radius:8px; border:1px solid var(--line); background: rgba(11,15,20,0.35); color: var(--text); cursor:pointer;">Open JSON file</button>'
        + '<div style="margin-top:8px;">Tip: If you prefer, run an HTTP server (e.g., VS Code Live Server) and reload.</div>'
      );

      const btn = document.getElementById('pick-json');
      if (btn) {
        btn.addEventListener('click', async () => {
          try {
            setStatus('Opening file picker…');
            const data = await loadDataViaFilePicker();
            setStatus('');
            renderMindmap(data);
          } catch (e) {
            console.error(e);
            setStatus(String(e?.message || e));
          }
        }, { once: true });
      }
      return;
    }

    setStatus(msg);
  }
})();

const projectFiles = [
  'aurora_atlas.json',
  'lumen_lab.json',
  'nebula_navigator.json',
  'nt-auth-test-212.json',
  'pixel_palette.json',
  'quantum_quill.json',
  'sentinel_sync.json'
];

const form = document.querySelector('[data-search-form]');
const input = document.querySelector('[data-search-input]');
const title = document.querySelector('[data-title]');
const list = document.querySelector('[data-list]');
const home = document.querySelector('[data-home]');
// Assets load relative to this page (codenest is a single-page app always served
// from codenest.com/index.html), so the site works under any hosting prefix:
// file:// (editor), the blob content host, or the same-origin /cq-web/ host.
const siteRoot = '';

function escapeText(value) {
  return String(value).replace(/[&<>"']/g, (character) => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
  })[character]);
}

function route() {
  const hash = location.hash.replace(/^#\/?/, '');
  const [path, queryString = ''] = hash.split('?');
  const params = new URLSearchParams(queryString);
  return {
    path,
    query: params.get('q') || ''
  };
}

async function loadProjects() {
  const projects = await Promise.all(
    projectFiles.map(async (file) => {
      const response = await fetch(`${siteRoot}assets/projects/${file}`);
      return response.json();
    })
  );
  return projects.sort((a, b) => a.name.localeCompare(b.name));
}

function projectCard(project) {
  return `<a class="project-card" href="#/project/${encodeURIComponent(project.id)}">
    <h2>${escapeText(project.name)}</h2>
    <p class="owner">@${escapeText(project.user)}</p>
    <p class="description">${escapeText(project.description)}</p>
  </a>`;
}

function renderHome(projects, query) {
  const trimmed = query.trim();
  input.value = trimmed;

  let visible = projects;
  if (trimmed) {
    const lower = trimmed.toLowerCase();
    visible = projects
      .filter((project) => project.name.toLowerCase().includes(lower))
      .sort((a, b) => {
        const nameA = a.name.toLowerCase();
        const nameB = b.name.toLowerCase();
        const indexA = nameA.indexOf(lower);
        const indexB = nameB.indexOf(lower);
        if (indexA !== indexB) return indexA - indexB;
        return nameA.length - nameB.length;
      });
    title.textContent = `Search results for "${trimmed}"`;
  } else {
    visible = projects.filter((project) => project.isPublic).slice(0, 5);
    title.textContent = 'Top public projects';
  }

  list.innerHTML = visible.length
    ? visible.map(projectCard).join('')
    : '<p class="empty">Nothing matched your search.</p>';
}

function renderProject(projects, projectId) {
  input.value = '';
  const project = projects.find((item) => item.id === projectId);
  if (!project) {
    title.textContent = 'Project not found.';
    list.innerHTML = '';
    return;
  }

  title.textContent = project.name;
  list.innerHTML = `<div class="project-meta">
    <p class="owner">@${escapeText(project.user)}</p>
    <p class="description">${escapeText(project.description)}</p>
  </div>
  <h2 class="files-title">Files</h2>
  ${project.files.map((file) => `<details class="file-card">
    <summary>${escapeText(file.name)}</summary>
    <pre>${escapeText(file.content)}</pre>
  </details>`).join('')}`;
}

async function render() {
  const projects = await loadProjects();
  const current = route();

  if (current.path.startsWith('project/')) {
    renderProject(projects, decodeURIComponent(current.path.slice('project/'.length)));
    return;
  }

  renderHome(projects, current.query);
}

form.addEventListener('submit', (event) => {
  event.preventDefault();
  const query = input.value.trim();
  location.hash = query ? `/search?q=${encodeURIComponent(query)}` : '/';
});

home.addEventListener('click', (event) => {
  event.preventDefault();
  location.hash = '/';
});

window.addEventListener('hashchange', () => {
  render().catch(() => {
    title.textContent = 'Failed to load projects.';
    list.innerHTML = '';
  });
});

render().catch(() => {
  title.textContent = 'Failed to load projects.';
  list.innerHTML = '';
});

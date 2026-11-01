(() => {
  "use strict";

  const LOOKUP_DELAY_MIN_MS = 400;
  const LOOKUP_DELAY_MAX_MS = 700;

  const form = document.getElementById("lookupForm");
  const input = document.getElementById("lookupInput");
  const button = document.getElementById("lookupButton");
  const result = document.getElementById("result");
  const status = document.getElementById("status");

  const samples = Array.from(document.querySelectorAll(".sample-btn"));

  /**
   * Normalize input for matching.
   * - trim
   * - lowercase
   */
  function normalize(raw) {
    return String(raw ?? "").trim().toLowerCase();
  }

  /**
   * Extract a "parsed domain" from either a domain or an email address.
   * If it looks like an email (contains @), return the part after the last @.
   */
  function parseDomain(normalized) {
    const at = normalized.lastIndexOf("@");
    if (at >= 0) return normalized.slice(at + 1);
    return normalized;
  }

  function randInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  }

  function setBusy(isBusy) {
    button.disabled = isBusy;
    input.disabled = isBusy;
    form.setAttribute("aria-busy", isBusy ? "true" : "false");
  }

  function setStatus(text) {
    status.textContent = text;
  }

  function escapeHtml(s) {
    return String(s)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  async function copyToClipboard(text) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      return false;
    }
  }

  function renderFallback() {
    // Exact required fallback string.
    result.innerHTML = `
      <div class="card">
        <p class="notice">No matching records found for that input. Try the sample inputs or check spelling.</p>
      </div>
    `;
  }

  function renderCard(data) {
    const subdomainItems = data.subdomains
      .map((d) => `<li>${escapeHtml(d)}</li>`)
      .join("");

    result.innerHTML = `
      <article class="card" aria-label="Lookup result card">
        <div class="card-top">
          <div class="match-pill">Input matched: ${escapeHtml(data.inputMatched)}</div>
          <div class="actions">
            <button class="copy-btn" type="button" data-copy="${escapeHtml(data.canonical)}">Copy canonical string</button>
            <span class="copy-hint sr-only" id="copyHelp">Copies the parsed domain to your clipboard.</span>
          </div>
        </div>

        <div class="grid">
          <div class="kv">
            <p class="k">Parsed domain</p>
            <p class="v">${escapeHtml(data.parsedDomain)}</p>
          </div>
          <div class="kv">
            <p class="k">Registrant / Organization</p>
            <p class="v">${escapeHtml(data.registrant)}</p>
          </div>
          <div class="kv">
            <p class="k">Hosting provider / ASN</p>
            <p class="v">${escapeHtml(data.hosting)}</p>
          </div>
          <div class="kv">
            <p class="k">Confidence / Last observed</p>
            <p class="v">${escapeHtml(data.confidenceLastObserved)}</p>
          </div>
          <div class="kv subdomains" style="grid-column: 1 / -1;">
            <p class="k">Discovered subdomains</p>
            <ul>${subdomainItems}</ul>
          </div>
        </div>

        <div class="interpretation" aria-label="Human interpretation">
          <p>${escapeHtml(data.interpretation)}</p>
        </div>
      </article>
    `;

    const copyBtn = result.querySelector(".copy-btn");
    copyBtn?.addEventListener("click", async () => {
      const canonical = copyBtn.getAttribute("data-copy") ?? "";
      const ok = await copyToClipboard(canonical);
      setStatus(ok ? "Copied canonical string to clipboard." : "Unable to copy. Clipboard access was blocked.");
    });
  }

  const canned = {
    // Result A — suspicious sender
    "notifications@summit-trust.secure-mail.com": {
      inputMatched: "notifications@summit-trust.secure-mail.com",
      parsedDomain: "summit-trust.secure-mail.com",
      registrant: "SecureMail Hosting Ltd",
      hosting: "CloudHorizon LLC",
      subdomains: ["summit-trust.secure-mail.com", "webmail.secure-mail.io"],
      confidenceLastObserved: "High confidence. Last observed 2 days ago.",
      interpretation:
        "The lookup lists SecureMail Hosting Ltd for this domain. It does not verify who authored the message or whether Summit Trust Bank authorized its use.",
      canonical: "summit-trust.secure-mail.com",
    },

    // Result B — official bank
    "summittrust.com": {
      inputMatched: "summittrust.com",
      parsedDomain: "summittrust.com",
      registrant: "Summit Trust Bank",
      hosting: "Summit Data Centers",
      subdomains: [
        "www.summittrust.com",
        "api.summittrust.com",
        "mail.summittrust.com",
      ],
      confidenceLastObserved: "High confidence. Last observed 1 day ago.",
      interpretation: "The lookup lists Summit Trust Bank as the registrant for this domain. Compare suspicious messages with the bank's published contact and reporting channels.",
      canonical: "summittrust.com",
    },
  };

  async function runLookup(raw) {
    const normalized = normalize(raw);
    if (!normalized) {
      renderFallback();
      return;
    }

    setStatus("");
    setBusy(true);

    // Simulate a short processing delay.
    const delay = randInt(LOOKUP_DELAY_MIN_MS, LOOKUP_DELAY_MAX_MS);
    await new Promise((r) => setTimeout(r, delay));

    const match = canned[normalized];
    if (match) {
      renderCard(match);
    } else {
      // Keep fiction; no real-world guidance.
      renderFallback();
    }

    setBusy(false);

    // Move keyboard focus to results for accessibility.
    result.focus();
  }

  form?.addEventListener("submit", (e) => {
    e.preventDefault();
    runLookup(input.value);
  });

  // Ensure Enter triggers submit naturally (and still works if form behavior changes).
  input?.addEventListener("keydown", (e) => {
    if (e.key === "Enter") {
      // Let the form submit handler run.
    }
  });

  samples.forEach((btn) => {
    btn.addEventListener("click", () => {
      const sample = btn.getAttribute("data-sample") ?? "";
      input.value = sample;
      input.focus();
      runLookup(sample);
    });
  });

  // Initialize.
  setBusy(false);
})();

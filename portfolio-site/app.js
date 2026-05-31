const demoData = {
  tasks: {
    eyebrow: "CRUD Demo",
    title: "Task Tracker",
    endpoint: "POST /tasks",
    resultTitle: "Current tasks",
    fields: [
      { name: "title", label: "Task", type: "text", value: "Prepare C# interview notes" },
      { name: "priority", label: "Priority", type: "select", options: ["High", "Medium", "Low"] },
      { name: "status", label: "Status", type: "select", options: ["Todo", "In Progress", "Done"] }
    ],
    items: [
      { title: "Build portfolio API", meta: "High / In Progress", accent: "teal" },
      { title: "Update CV", meta: "Medium / Todo", accent: "amber" }
    ],
    create(values, state) {
      state.items.unshift({ title: values.title, meta: `${values.priority} / ${values.status}`, accent: values.priority === "High" ? "rose" : "teal" });
    }
  },
  expenses: {
    eyebrow: "Reports Demo",
    title: "Expense Tracker",
    endpoint: "POST /expenses",
    resultTitle: "Budget summary",
    fields: [
      { name: "name", label: "Expense", type: "text", value: "Online C# course" },
      { name: "category", label: "Category", type: "select", options: ["Education", "Transport", "Food", "Tools"] },
      { name: "amount", label: "Amount", type: "number", value: "249.99" }
    ],
    items: [
      { title: "Education", meta: "R249.99", accent: "teal" },
      { title: "Transport", meta: "R320.00", accent: "amber" }
    ],
    create(values, state) {
      const amount = Number(values.amount || 0).toFixed(2);
      state.items.unshift({ title: values.category, meta: `R${amount} - ${values.name}`, accent: "teal" });
    }
  },
  library: {
    eyebrow: "Business Rules Demo",
    title: "Library Loan Manager",
    endpoint: "POST /loans",
    resultTitle: "Active library records",
    fields: [
      { name: "book", label: "Book title", type: "text", value: "Head First C#" },
      { name: "member", label: "Member", type: "text", value: "Amina Jacobs" },
      { name: "days", label: "Loan days", type: "number", value: "14" }
    ],
    items: [
      { title: "Clean Code", meta: "Available", accent: "teal" },
      { title: "The Pragmatic Programmer", meta: "On loan to Sipho", accent: "rose" }
    ],
    create(values, state) {
      state.items.unshift({ title: values.book, meta: `On loan to ${values.member} for ${values.days} days`, accent: "rose" });
    }
  },
  jobs: {
    eyebrow: "Workflow Demo",
    title: "Job Application Tracker",
    endpoint: "POST /applications",
    resultTitle: "Applications pipeline",
    fields: [
      { name: "role", label: "Role", type: "text", value: "Junior Backend Developer" },
      { name: "company", label: "Company", type: "text", value: "Example Software" },
      { name: "status", label: "Status", type: "select", options: ["Applied", "Interview", "Offer", "Rejected"] }
    ],
    items: [
      { title: "Junior C# Developer", meta: "BrightApps / Applied", accent: "amber" },
      { title: "Graduate Software Developer", meta: "CloudBridge / Interview", accent: "teal" }
    ],
    create(values, state) {
      state.items.unshift({ title: values.role, meta: `${values.company} / ${values.status}`, accent: values.status === "Interview" ? "teal" : "amber" });
    }
  },
  weather: {
    eyebrow: "Analytics Demo",
    title: "Weather Journal",
    endpoint: "POST /entries",
    resultTitle: "Weather entries",
    fields: [
      { name: "city", label: "City", type: "text", value: "Pretoria" },
      { name: "temperature", label: "Temperature C", type: "number", value: "20.7" },
      { name: "condition", label: "Condition", type: "select", options: ["Clear", "Cloudy", "Humid", "Rain"] }
    ],
    items: [
      { title: "Johannesburg", meta: "18.2 C / Clear", accent: "teal" },
      { title: "Durban", meta: "26.8 C / Humid", accent: "amber" }
    ],
    create(values, state) {
      state.items.unshift({ title: values.city, meta: `${values.temperature} C / ${values.condition}`, accent: Number(values.temperature) > 25 ? "amber" : "teal" });
    }
  }
};

const advancedDemoData = {
  service: {
    eyebrow: "Workflow Simulator",
    title: "Service Desk Simulator",
    endpoint: "POST /tickets",
    summary: "Create support tickets, assign agents, and watch SLA pressure change in the dashboard.",
    resultTitle: "Ticket queue",
    fields: [
      { name: "title", label: "Issue", type: "text", value: "Customer cannot access booking portal" },
      { name: "priority", label: "Priority", type: "select", options: ["Low", "Medium", "High", "Critical"] },
      { name: "agent", label: "Assigned agent", type: "select", options: ["Oageng", "Support Team", "Infrastructure", "Unassigned"] }
    ],
    records: [
      { title: "Payment receipt email failed", meta: "High / In Progress / Oageng", detail: "SLA: 4h remaining", status: "In Progress", priority: "High", accent: "amber" },
      { title: "Login page returns timeout", meta: "Critical / New / Infrastructure", detail: "SLA: 1h remaining", status: "New", priority: "Critical", accent: "rose" },
      { title: "Update user contact details", meta: "Medium / Resolved / Support Team", detail: "Resolved with audit trail", status: "Resolved", priority: "Medium", accent: "teal" }
    ],
    create(values, state) {
      const sla = values.priority === "Critical" ? "1h" : values.priority === "High" ? "4h" : values.priority === "Medium" ? "1 day" : "3 days";
      state.records.unshift({
        title: values.title,
        meta: `${values.priority} / New / ${values.agent}`,
        detail: `SLA target: ${sla} - audit event created`,
        status: "New",
        priority: values.priority,
        accent: values.priority === "Critical" ? "rose" : values.priority === "High" ? "amber" : "teal"
      });
      state.notice = { title: "Ticket created", text: "Validation, assignment, SLA target, and audit log were applied.", accent: "teal" };
    },
    metrics(state) {
      const open = state.records.filter((item) => item.status !== "Resolved").length;
      const critical = state.records.filter((item) => item.priority === "Critical").length;
      const slaWatch = state.records.filter((item) => ["Critical", "High"].includes(item.priority) && item.status !== "Resolved").length;
      return [
        { label: "Open tickets", value: open },
        { label: "Critical", value: critical },
        { label: "SLA watch", value: slaWatch }
      ];
    },
    results(state) {
      return state.records;
    }
  },
  inventory: {
    eyebrow: "Business Rules Simulator",
    title: "Inventory Orders Simulator",
    endpoint: "POST /orders",
    summary: "Place customer orders, reduce stock, calculate VAT-ready totals, and trigger low-stock warnings.",
    resultTitle: "Orders and stock rules",
    fields: [
      { name: "product", label: "Product", type: "select", options: ["API Integration Package", "Booking Kiosk License", "Support Retainer"] },
      { name: "quantity", label: "Quantity", type: "number", value: "2" },
      { name: "customer", label: "Customer", type: "text", value: "Pretoria Skills Hub" }
    ],
    products: [
      { name: "API Integration Package", stock: 9, price: 1850 },
      { name: "Booking Kiosk License", stock: 4, price: 3200 },
      { name: "Support Retainer", stock: 12, price: 950 }
    ],
    records: [
      { title: "Booking Kiosk License", meta: "1 unit / R3,200.00 / Nkosi Events", detail: "Stock released from inventory", accent: "teal", total: 3200, quantity: 1 },
      { title: "Support Retainer", meta: "3 units / R2,850.00 / ByteCare", detail: "Recurring support order", accent: "amber", total: 2850, quantity: 3 }
    ],
    create(values, state) {
      const quantity = Math.max(Number(values.quantity || 0), 0);
      const product = state.products.find((item) => item.name === values.product);
      if (!product || quantity <= 0) {
        state.notice = { title: "Order rejected", text: "Quantity must be greater than zero.", accent: "rose" };
        return;
      }

      if (product.stock < quantity) {
        state.notice = { title: "Stock warning", text: `${product.name} only has ${product.stock} units available.`, accent: "rose" };
        return;
      }

      product.stock -= quantity;
      const total = product.price * quantity;
      state.records.unshift({
        title: product.name,
        meta: `${quantity} units / ${formatCurrency(total)} / ${values.customer}`,
        detail: `${product.stock} left in stock after order`,
        accent: product.stock <= 3 ? "amber" : "teal",
        total,
        quantity
      });
      state.notice = { title: "Order placed", text: "Stock, order total, and sales report figures were updated.", accent: "teal" };
    },
    metrics(state) {
      const revenue = state.records.reduce((sum, item) => sum + Number(item.total || 0), 0);
      const units = state.records.reduce((sum, item) => sum + Number(item.quantity || 0), 0);
      const lowStock = state.products.filter((item) => item.stock <= 4).length;
      return [
        { label: "Revenue", value: formatCurrency(revenue) },
        { label: "Units sold", value: units },
        { label: "Low stock", value: lowStock }
      ];
    },
    results(state) {
      const stockRows = state.products.map((item) => ({
        title: item.name,
        meta: `${item.stock} in stock / ${formatCurrency(item.price)} each`,
        detail: item.stock <= 4 ? "Low-stock alert active" : "Stock level healthy",
        accent: item.stock <= 4 ? "amber" : "teal"
      }));
      return [...state.records, ...stockRows];
    }
  },
  learning: {
    eyebrow: "Analytics Simulator",
    title: "Learning Progress Simulator",
    endpoint: "POST /quiz-submissions",
    summary: "Submit quiz results, calculate pass rules, update learner progress, and monitor course performance.",
    resultTitle: "Learner dashboard",
    fields: [
      { name: "learner", label: "Learner", type: "text", value: "Oageng Tsumaki" },
      { name: "course", label: "Course", type: "select", options: ["C# Web API Fundamentals", "SQL Reporting Basics", "Frontend Portfolio Polish"] },
      { name: "score", label: "Quiz score", type: "number", value: "82" }
    ],
    records: [
      { title: "C# Web API Fundamentals", meta: "Thabo M. / 88% / Passed", detail: "Certificate ready", score: 88, passed: true, accent: "teal" },
      { title: "SQL Reporting Basics", meta: "Amina J. / 69% / Revision needed", detail: "Module retry suggested", score: 69, passed: false, accent: "amber" },
      { title: "Frontend Portfolio Polish", meta: "Lerato K. / 91% / Passed", detail: "Advanced animation module complete", score: 91, passed: true, accent: "teal" }
    ],
    create(values, state) {
      const score = Math.min(Math.max(Number(values.score || 0), 0), 100);
      const passed = score >= 75;
      state.records.unshift({
        title: values.course,
        meta: `${values.learner} / ${score}% / ${passed ? "Passed" : "Revision needed"}`,
        detail: passed ? "Certificate rule passed" : "Learner dashboard marked for follow-up",
        score,
        passed,
        accent: passed ? "teal" : "amber"
      });
      state.notice = { title: passed ? "Learner passed" : "Revision flagged", text: "The dashboard metrics and course performance report were recalculated.", accent: passed ? "teal" : "amber" };
    },
    metrics(state) {
      const count = state.records.length;
      const passed = state.records.filter((item) => item.passed).length;
      const average = count ? Math.round(state.records.reduce((sum, item) => sum + Number(item.score || 0), 0) / count) : 0;
      return [
        { label: "Learners", value: count },
        { label: "Completed", value: passed },
        { label: "Avg score", value: `${average}%` }
      ];
    },
    results(state) {
      return state.records;
    }
  }
};

function cloneConfig(config) {
  return {
    ...config,
    items: config.items ? JSON.parse(JSON.stringify(config.items)) : undefined,
    records: config.records ? JSON.parse(JSON.stringify(config.records)) : undefined,
    products: config.products ? JSON.parse(JSON.stringify(config.products)) : undefined,
    notice: null
  };
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function formatCurrency(value) {
  return new Intl.NumberFormat("en-ZA", {
    style: "currency",
    currency: "ZAR"
  }).format(value);
}

const state = Object.fromEntries(
  Object.entries(demoData).map(([key, config]) => [
    key,
    cloneConfig(config)
  ])
);

const advancedState = Object.fromEntries(
  Object.entries(advancedDemoData).map(([key, config]) => [
    key,
    cloneConfig(config)
  ])
);
let activeDemo = "tasks";
let activeAdvancedDemo = "service";

const form = document.querySelector("#demoForm");
const results = document.querySelector("#demoResults");
const title = document.querySelector("#demoTitle");
const eyebrow = document.querySelector("#demoEyebrow");
const endpoint = document.querySelector("#demoEndpoint");
const resultTitle = document.querySelector("#resultTitle");
const advancedForm = document.querySelector("#advancedForm");
const advancedResults = document.querySelector("#advancedResults");
const advancedMetrics = document.querySelector("#advancedMetrics");
const advancedTitle = document.querySelector("#advancedTitle");
const advancedEyebrow = document.querySelector("#advancedEyebrow");
const advancedEndpoint = document.querySelector("#advancedEndpoint");
const advancedSummary = document.querySelector("#advancedSummary");
const advancedResultTitle = document.querySelector("#advancedResultTitle");

function renderForm(config, targetForm = form, submitLabel = "Add record") {
  targetForm.innerHTML = config.fields.map((field) => {
    if (field.type === "select") {
      return `
        <label>
          <span>${escapeHtml(field.label)}</span>
          <select name="${escapeHtml(field.name)}">
            ${field.options.map((option) => `<option>${escapeHtml(option)}</option>`).join("")}
          </select>
        </label>
      `;
    }

    return `
      <label>
        <span>${escapeHtml(field.label)}</span>
        <input name="${escapeHtml(field.name)}" type="${escapeHtml(field.type)}" value="${escapeHtml(field.value ?? "")}" required>
      </label>
    `;
  }).join("") + `<button class="button primary" type="submit">${escapeHtml(submitLabel)}</button>`;
}

function renderResults(config) {
  results.innerHTML = config.items.map((item, index) => `
    <article class="result-card ${escapeHtml(item.accent)}" style="--delay: ${index * 60}ms">
      <span></span>
      <div>
        <strong>${escapeHtml(item.title)}</strong>
        <p>${escapeHtml(item.meta)}</p>
      </div>
    </article>
  `).join("");
}

function renderDemo(name) {
  activeDemo = name;
  const config = state[name];
  eyebrow.textContent = config.eyebrow;
  title.textContent = config.title;
  endpoint.textContent = config.endpoint;
  resultTitle.textContent = config.resultTitle;
  renderForm(config);
  renderResults(config);
}

function renderAdvancedMetrics(config) {
  advancedMetrics.innerHTML = config.metrics(config).map((metric) => `
    <article class="advanced-stat">
      <strong>${escapeHtml(metric.value)}</strong>
      <span>${escapeHtml(metric.label)}</span>
    </article>
  `).join("");
}

function renderAdvancedResults(config) {
  const notice = config.notice ? `
    <article class="result-card advanced-notice ${escapeHtml(config.notice.accent)}" style="--delay: 0ms">
      <span></span>
      <div>
        <strong>${escapeHtml(config.notice.title)}</strong>
        <p>${escapeHtml(config.notice.text)}</p>
      </div>
    </article>
  ` : "";

  advancedResults.innerHTML = notice + config.results(config).map((item, index) => `
    <article class="result-card ${escapeHtml(item.accent)}" style="--delay: ${(index + 1) * 55}ms">
      <span></span>
      <div>
        <strong>${escapeHtml(item.title)}</strong>
        <p>${escapeHtml(item.meta)}</p>
        <small>${escapeHtml(item.detail)}</small>
      </div>
    </article>
  `).join("");
}

function renderAdvancedDemo(name) {
  activeAdvancedDemo = name;
  const config = advancedState[name];
  advancedEyebrow.textContent = config.eyebrow;
  advancedTitle.textContent = config.title;
  advancedEndpoint.textContent = config.endpoint;
  advancedSummary.textContent = config.summary;
  advancedResultTitle.textContent = config.resultTitle;
  renderAdvancedMetrics(config);
  renderForm(config, advancedForm, "Run workflow");
  renderAdvancedResults(config);
}

document.querySelectorAll(".tab-button").forEach((button) => {
  button.addEventListener("click", () => {
    document.querySelectorAll(".tab-button").forEach((tab) => tab.classList.remove("active"));
    button.classList.add("active");
    renderDemo(button.dataset.demo);
  });
});

document.querySelectorAll(".advanced-tab").forEach((button) => {
  button.addEventListener("click", () => {
    document.querySelectorAll(".advanced-tab").forEach((tab) => tab.classList.remove("active"));
    button.classList.add("active");
    renderAdvancedDemo(button.dataset.advancedDemo);
  });
});

form.addEventListener("submit", (event) => {
  event.preventDefault();
  const values = Object.fromEntries(new FormData(form).entries());
  state[activeDemo].create(values, state[activeDemo]);
  renderResults(state[activeDemo]);
  form.animate(
    [
      { transform: "translateY(0)" },
      { transform: "translateY(-4px)" },
      { transform: "translateY(0)" }
    ],
    { duration: 220, easing: "ease-out" }
  );
});

document.querySelector("#resetDemo").addEventListener("click", () => {
  state[activeDemo].items = JSON.parse(JSON.stringify(demoData[activeDemo].items));
  renderResults(state[activeDemo]);
});

advancedForm.addEventListener("submit", (event) => {
  event.preventDefault();
  const values = Object.fromEntries(new FormData(advancedForm).entries());
  advancedState[activeAdvancedDemo].create(values, advancedState[activeAdvancedDemo]);
  renderAdvancedMetrics(advancedState[activeAdvancedDemo]);
  renderAdvancedResults(advancedState[activeAdvancedDemo]);
  advancedForm.animate(
    [
      { transform: "translateY(0)" },
      { transform: "translateY(-4px)" },
      { transform: "translateY(0)" }
    ],
    { duration: 220, easing: "ease-out" }
  );
});

document.querySelector("#resetAdvancedDemo").addEventListener("click", () => {
  advancedState[activeAdvancedDemo] = cloneConfig(advancedDemoData[activeAdvancedDemo]);
  renderAdvancedDemo(activeAdvancedDemo);
});

const revealObserver = new IntersectionObserver((entries) => {
  entries.forEach((entry) => {
    if (entry.isIntersecting) {
      entry.target.classList.add("is-visible");
      revealObserver.unobserve(entry.target);
    }
  });
}, { threshold: 0.14 });

document.querySelectorAll(".reveal").forEach((element) => revealObserver.observe(element));

const countObserver = new IntersectionObserver((entries) => {
  entries.forEach((entry) => {
    if (!entry.isIntersecting) {
      return;
    }

    const element = entry.target;
    const target = Number(element.dataset.count);
    const start = performance.now();
    const duration = 900;

    function tick(now) {
      const progress = Math.min((now - start) / duration, 1);
      element.textContent = Math.round(target * progress);
      if (progress < 1) {
        requestAnimationFrame(tick);
      }
    }

    requestAnimationFrame(tick);
    countObserver.unobserve(element);
  });
}, { threshold: 0.7 });

document.querySelectorAll("[data-count]").forEach((element) => countObserver.observe(element));

document.querySelectorAll(".magnetic").forEach((button) => {
  button.addEventListener("pointermove", (event) => {
    const rect = button.getBoundingClientRect();
    const x = event.clientX - rect.left - rect.width / 2;
    const y = event.clientY - rect.top - rect.height / 2;
    button.style.transform = `translate(${x * 0.08}px, ${y * 0.12}px)`;
  });

  button.addEventListener("pointerleave", () => {
    button.style.transform = "translate(0, 0)";
  });
});

renderDemo(activeDemo);
renderAdvancedDemo(activeAdvancedDemo);

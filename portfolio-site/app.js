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

const state = Object.fromEntries(
  Object.entries(demoData).map(([key, config]) => [
    key,
    { ...config, items: JSON.parse(JSON.stringify(config.items)) }
  ])
);
let activeDemo = "tasks";

const form = document.querySelector("#demoForm");
const results = document.querySelector("#demoResults");
const title = document.querySelector("#demoTitle");
const eyebrow = document.querySelector("#demoEyebrow");
const endpoint = document.querySelector("#demoEndpoint");
const resultTitle = document.querySelector("#resultTitle");

function renderForm(config) {
  form.innerHTML = config.fields.map((field) => {
    if (field.type === "select") {
      return `
        <label>
          <span>${field.label}</span>
          <select name="${field.name}">
            ${field.options.map((option) => `<option>${option}</option>`).join("")}
          </select>
        </label>
      `;
    }

    return `
      <label>
        <span>${field.label}</span>
        <input name="${field.name}" type="${field.type}" value="${field.value ?? ""}" required>
      </label>
    `;
  }).join("") + '<button class="button primary" type="submit">Add record</button>';
}

function renderResults(config) {
  results.innerHTML = config.items.map((item, index) => `
    <article class="result-card ${item.accent}" style="--delay: ${index * 60}ms">
      <span></span>
      <div>
        <strong>${item.title}</strong>
        <p>${item.meta}</p>
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

document.querySelectorAll(".tab-button").forEach((button) => {
  button.addEventListener("click", () => {
    document.querySelectorAll(".tab-button").forEach((tab) => tab.classList.remove("active"));
    button.classList.add("active");
    renderDemo(button.dataset.demo);
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

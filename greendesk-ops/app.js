const initialState = {
  tickets: [
    {
      id: 1,
      title: "VPN access fails after password reset",
      description: "Finance user cannot connect after resetting their password.",
      department: "Finance",
      requester: "Amina Jacobs",
      priority: "High",
      status: "InProgress",
      assignedTo: "Oageng",
      slaHours: 4,
      minutes: 35
    },
    {
      id: 2,
      title: "Reception tablet battery swelling",
      description: "Front desk check-in tablet is overheating and needs replacement review.",
      department: "Front Desk",
      requester: "Kefilewe",
      priority: "Critical",
      status: "Assigned",
      assignedTo: "Infrastructure",
      slaHours: 2,
      minutes: 25
    },
    {
      id: 3,
      title: "New starter laptop setup",
      description: "Prepare laptop, accounts, email, browser bookmarks, and security checklist.",
      department: "Operations",
      requester: "Johan",
      priority: "Medium",
      status: "Resolved",
      assignedTo: "Oageng",
      slaHours: -10,
      minutes: 80
    }
  ],
  assets: [
    { id: 1, tag: "GD-LTP-014", name: "Lenovo ThinkPad E14", category: "Laptop", assignedTo: "Finance Desk", location: "Pretoria East", health: "Healthy", cost: 14500, energy: 82 },
    { id: 2, tag: "GD-TAB-003", name: "Samsung Check-in Tablet", category: "Tablet", assignedTo: "Reception", location: "Front Desk", health: "NeedsRepair", cost: 6200, energy: 58 },
    { id: 3, tag: "GD-RTR-002", name: "Ubiquiti Edge Router", category: "Network", assignedTo: "Infrastructure", location: "Server Room", health: "Watch", cost: 3900, energy: 74 },
    { id: 4, tag: "GD-MON-021", name: "Dell 24-inch Monitor", category: "Display", assignedTo: "Operations", location: "Training Room", health: "Healthy", cost: 3200, energy: 88 }
  ],
  maintenance: [
    { id: 1, assetId: 2, assetName: "Samsung Check-in Tablet", task: "Replace swollen battery and run safety check", status: "InProgress", dueDays: 1, owner: "Infrastructure", risk: "High" },
    { id: 2, assetId: 3, assetName: "Ubiquiti Edge Router", task: "Review firmware version and backup configuration", status: "Planned", dueDays: 5, owner: "Oageng", risk: "Medium" },
    { id: 3, assetId: 1, assetName: "Lenovo ThinkPad E14", task: "Quarterly patch and endpoint health check", status: "Planned", dueDays: 12, owner: "Oageng", risk: "Low" }
  ],
  activity: [
    "Critical tablet issue assigned to Infrastructure.",
    "Router health moved to Watch after packet loss review.",
    "Battery replacement task moved to In Progress.",
    "New starter setup resolved with handover checklist."
  ]
};

const formModes = {
  ticket: {
    eyebrow: "POST /tickets",
    title: "Create a Service Ticket",
    hint: "SLA is calculated from priority",
    button: "Create Ticket",
    fields: [
      { name: "title", label: "Issue title", type: "text", value: "Printer queue stuck after network change", className: "wide" },
      { name: "department", label: "Department", type: "select", options: ["Operations", "Finance", "Front Desk", "Infrastructure", "Training"] },
      { name: "requester", label: "Requester", type: "text", value: "Phillip" },
      { name: "priority", label: "Priority", type: "select", options: ["Low", "Medium", "High", "Critical"] },
      { name: "assignedTo", label: "Assign to", type: "select", options: ["Oageng", "Infrastructure", "Support Team", "Unassigned"] },
      { name: "description", label: "Description", type: "textarea", value: "Operations printer has six stuck jobs and staff cannot print dispatch forms.", className: "full" }
    ]
  },
  asset: {
    eyebrow: "POST /assets",
    title: "Register an Asset",
    hint: "Asset tags are normalized for clean records",
    button: "Register Asset",
    fields: [
      { name: "tag", label: "Asset tag", type: "text", value: "GD-LTP-030" },
      { name: "name", label: "Asset name", type: "text", value: "Dell Latitude 5440" },
      { name: "category", label: "Category", type: "select", options: ["Laptop", "Tablet", "Network", "Display", "Printer"] },
      { name: "assignedTo", label: "Assigned to", type: "text", value: "New Developer" },
      { name: "location", label: "Location", type: "text", value: "Pretoria East" },
      { name: "cost", label: "Replacement cost", type: "number", value: "16800" },
      { name: "energy", label: "Energy rating", type: "number", value: "89" }
    ]
  },
  maintenance: {
    eyebrow: "POST /maintenance",
    title: "Schedule Maintenance",
    hint: "Maintenance connects asset risk to planned work",
    button: "Schedule Work",
    fields: [
      { name: "assetId", label: "Asset", type: "asset-select" },
      { name: "owner", label: "Owner", type: "select", options: ["Oageng", "Infrastructure", "Support Team", "Vendor"] },
      { name: "risk", label: "Risk", type: "select", options: ["Low", "Medium", "High"] },
      { name: "dueDays", label: "Due in days", type: "number", value: "7" },
      { name: "task", label: "Task", type: "textarea", value: "Run endpoint health check and document maintenance notes.", className: "full" }
    ]
  }
};

let state = clone(initialState);
let activeMode = "ticket";

const formatter = new Intl.NumberFormat("en-ZA", { style: "currency", currency: "ZAR", maximumFractionDigits: 0 });

const elements = {
  form: document.querySelector("#opsForm"),
  formEyebrow: document.querySelector("#formEyebrow"),
  formTitle: document.querySelector("#formTitle"),
  formHint: document.querySelector("#formHint"),
  toast: document.querySelector("#toast"),
  ticketGrid: document.querySelector("#ticketGrid"),
  assetGrid: document.querySelector("#assetGrid"),
  maintenanceList: document.querySelector("#maintenanceList"),
  activityList: document.querySelector("#activityList"),
  departmentChart: document.querySelector("#departmentChart"),
  ticketFilter: document.querySelector("#ticketFilter")
};

function clone(value) {
  return JSON.parse(JSON.stringify(value));
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function isOpen(ticket) {
  return !["Resolved", "Closed"].includes(ticket.status);
}

function isSlaRisk(ticket) {
  return isOpen(ticket) && Number(ticket.slaHours) <= 6;
}

function healthBadge(health) {
  if (health === "Healthy") return "green";
  if (health === "Watch") return "warning";
  return "danger";
}

function priorityBadge(priority) {
  if (priority === "Critical") return "danger";
  if (priority === "High") return "warning";
  return "green";
}

function pushActivity(message) {
  state.activity.unshift(message);
  state.activity = state.activity.slice(0, 8);
}

function nextId(items) {
  return Math.max(0, ...items.map((item) => item.id)) + 1;
}

function renderForm() {
  const mode = formModes[activeMode];
  elements.formEyebrow.textContent = mode.eyebrow;
  elements.formTitle.textContent = mode.title;
  elements.formHint.textContent = mode.hint;

  elements.form.innerHTML = mode.fields.map((field) => {
    const className = field.className ? ` class="${field.className}"` : "";
    if (field.type === "select") {
      return `<label${className}><span>${field.label}</span><select name="${field.name}">${field.options.map((option) => `<option>${option}</option>`).join("")}</select></label>`;
    }

    if (field.type === "asset-select") {
      return `<label${className}><span>${field.label}</span><select name="${field.name}">${state.assets.map((asset) => `<option value="${asset.id}">${escapeHtml(asset.tag)} - ${escapeHtml(asset.name)}</option>`).join("")}</select></label>`;
    }

    if (field.type === "textarea") {
      return `<label${className}><span>${field.label}</span><textarea name="${field.name}" required>${escapeHtml(field.value ?? "")}</textarea></label>`;
    }

    return `<label${className}><span>${field.label}</span><input name="${field.name}" type="${field.type}" value="${escapeHtml(field.value ?? "")}" required></label>`;
  }).join("") + `<button class="button primary" type="submit">${mode.button}</button>`;
}

function renderMetrics() {
  const openTickets = state.tickets.filter(isOpen).length;
  const slaRisk = state.tickets.filter(isSlaRisk).length;
  const assetWatch = state.assets.filter((asset) => ["Watch", "NeedsRepair"].includes(asset.health)).length;
  const assetValue = state.assets.filter((asset) => asset.health !== "Retired").reduce((sum, asset) => sum + Number(asset.cost), 0);

  document.querySelector("#openTickets").textContent = openTickets;
  document.querySelector("#slaRisk").textContent = slaRisk;
  document.querySelector("#assetWatch").textContent = assetWatch;
  document.querySelector("#assetValue").textContent = formatter.format(assetValue);
}

function renderTickets() {
  const filter = elements.ticketFilter.value;
  let tickets = [...state.tickets];

  if (filter === "risk") tickets = tickets.filter(isSlaRisk);
  if (filter === "critical") tickets = tickets.filter((ticket) => ticket.priority === "Critical");
  if (filter === "open") tickets = tickets.filter(isOpen);

  elements.ticketGrid.innerHTML = tickets.map((ticket) => `
    <article class="ticket-card">
      <div class="ticket-top">
        <span class="badge ${priorityBadge(ticket.priority)}">${ticket.priority}</span>
        <span class="badge ${isSlaRisk(ticket) ? "danger" : "green"}">${isSlaRisk(ticket) ? "SLA Risk" : ticket.status}</span>
      </div>
      <h3>${escapeHtml(ticket.title)}</h3>
      <p>${escapeHtml(ticket.department)} / ${escapeHtml(ticket.requester)} / ${escapeHtml(ticket.assignedTo)}</p>
      <p>${escapeHtml(ticket.description)}</p>
      <div class="ticket-actions">
        <button class="mini-button" type="button" data-ticket-action="progress" data-id="${ticket.id}">Progress</button>
        <button class="mini-button" type="button" data-ticket-action="resolve" data-id="${ticket.id}">Resolve</button>
        <button class="mini-button" type="button" data-ticket-action="log" data-id="${ticket.id}">Log 15m</button>
      </div>
    </article>
  `).join("");
}

function renderAssets() {
  elements.assetGrid.innerHTML = state.assets.map((asset) => `
    <article class="asset-card">
      <div class="asset-top">
        <span class="badge">${escapeHtml(asset.tag)}</span>
        <span class="badge ${healthBadge(asset.health)}">${escapeHtml(asset.health)}</span>
      </div>
      <h3>${escapeHtml(asset.name)}</h3>
      <p>${escapeHtml(asset.category)} / ${escapeHtml(asset.assignedTo)} / ${escapeHtml(asset.location)}</p>
      <p>${formatter.format(asset.cost)} replacement / ${asset.energy}% energy rating</p>
      <div class="ticket-actions">
        <button class="mini-button" type="button" data-asset-action="watch" data-id="${asset.id}">Watch</button>
        <button class="mini-button" type="button" data-asset-action="healthy" data-id="${asset.id}">Healthy</button>
      </div>
    </article>
  `).join("");
}

function renderMaintenance() {
  elements.maintenanceList.innerHTML = state.maintenance.map((task) => `
    <article class="maintenance-card">
      <div class="maintenance-top">
        <strong>${escapeHtml(task.assetName)}</strong>
        <span class="badge ${task.risk === "High" ? "danger" : task.risk === "Medium" ? "warning" : "green"}">${escapeHtml(task.risk)}</span>
      </div>
      <p>${escapeHtml(task.task)}</p>
      <p>${escapeHtml(task.owner)} / ${escapeHtml(task.status)} / due in ${task.dueDays} days</p>
      <div class="maintenance-actions">
        <button class="mini-button" type="button" data-maintenance-action="start" data-id="${task.id}">Start</button>
        <button class="mini-button" type="button" data-maintenance-action="complete" data-id="${task.id}">Complete</button>
      </div>
    </article>
  `).join("");
}

function renderActivity() {
  elements.activityList.innerHTML = state.activity.map((message) => `
    <article class="activity-item">
      <p>${escapeHtml(message)}</p>
    </article>
  `).join("");
}

function renderChart() {
  const counts = state.tickets.reduce((grouped, ticket) => {
    grouped[ticket.department] = (grouped[ticket.department] || 0) + 1;
    return grouped;
  }, {});
  const max = Math.max(1, ...Object.values(counts));

  elements.departmentChart.innerHTML = Object.entries(counts).map(([department, count]) => `
    <div class="bar-row">
      <strong>${escapeHtml(department)}</strong>
      <span class="bar-track"><span class="bar-fill" style="width:${(count / max) * 100}%"></span></span>
      <span>${count}</span>
    </div>
  `).join("");
}

function renderAll() {
  renderMetrics();
  renderTickets();
  renderAssets();
  renderMaintenance();
  renderActivity();
  renderChart();
  renderForm();
}

function showToast(message) {
  elements.toast.textContent = message;
  elements.toast.animate(
    [{ transform: "translateY(0)" }, { transform: "translateY(-4px)" }, { transform: "translateY(0)" }],
    { duration: 220, easing: "ease-out" }
  );
}

function createTicket(values) {
  const slaHours = values.priority === "Critical" ? 4 : values.priority === "High" ? 12 : values.priority === "Medium" ? 48 : 120;
  state.tickets.unshift({
    id: nextId(state.tickets),
    title: values.title,
    description: values.description,
    department: values.department,
    requester: values.requester,
    priority: values.priority,
    status: values.assignedTo === "Unassigned" ? "New" : "Assigned",
    assignedTo: values.assignedTo,
    slaHours,
    minutes: 0
  });
  pushActivity(`${values.priority} ticket opened for ${values.department}: ${values.title}`);
  showToast("Ticket created and dashboard metrics updated.");
}

function createAsset(values) {
  state.assets.unshift({
    id: nextId(state.assets),
    tag: values.tag.toUpperCase(),
    name: values.name,
    category: values.category,
    assignedTo: values.assignedTo,
    location: values.location,
    health: "Healthy",
    cost: Number(values.cost || 0),
    energy: Math.min(Math.max(Number(values.energy || 0), 0), 100)
  });
  pushActivity(`${values.tag.toUpperCase()} registered in the asset inventory.`);
  showToast("Asset registered with healthy default status.");
}

function createMaintenance(values) {
  const asset = state.assets.find((item) => item.id === Number(values.assetId));
  state.maintenance.unshift({
    id: nextId(state.maintenance),
    assetId: asset.id,
    assetName: asset.name,
    task: values.task,
    status: "Planned",
    dueDays: Number(values.dueDays || 7),
    owner: values.owner,
    risk: values.risk
  });
  pushActivity(`Maintenance scheduled for ${asset.tag}: ${values.task}`);
  showToast("Maintenance task scheduled.");
}

document.querySelectorAll(".mode-button").forEach((button) => {
  button.addEventListener("click", () => {
    document.querySelectorAll(".mode-button").forEach((item) => item.classList.remove("active"));
    button.classList.add("active");
    activeMode = button.dataset.mode;
    renderForm();
  });
});

elements.form.addEventListener("submit", (event) => {
  event.preventDefault();
  const values = Object.fromEntries(new FormData(elements.form).entries());
  if (activeMode === "ticket") createTicket(values);
  if (activeMode === "asset") createAsset(values);
  if (activeMode === "maintenance") createMaintenance(values);
  renderAll();
});

elements.ticketGrid.addEventListener("click", (event) => {
  const button = event.target.closest("[data-ticket-action]");
  if (!button) return;
  const ticket = state.tickets.find((item) => item.id === Number(button.dataset.id));
  if (!ticket) return;

  if (button.dataset.ticketAction === "progress" && ticket.status !== "Resolved") {
    ticket.status = "InProgress";
    pushActivity(`Ticket #${ticket.id} moved to In Progress.`);
    showToast("Ticket workflow moved forward.");
  }

  if (button.dataset.ticketAction === "resolve") {
    ticket.status = "Resolved";
    ticket.slaHours = -1;
    pushActivity(`Ticket #${ticket.id} resolved.`);
    showToast("Ticket resolved and removed from open count.");
  }

  if (button.dataset.ticketAction === "log") {
    ticket.minutes += 15;
    pushActivity(`15 minutes logged on ticket #${ticket.id}.`);
    showToast("Work log added.");
  }

  renderAll();
});

elements.assetGrid.addEventListener("click", (event) => {
  const button = event.target.closest("[data-asset-action]");
  if (!button) return;
  const asset = state.assets.find((item) => item.id === Number(button.dataset.id));
  if (!asset) return;
  asset.health = button.dataset.assetAction === "watch" ? "Watch" : "Healthy";
  pushActivity(`${asset.tag} health changed to ${asset.health}.`);
  showToast("Asset health updated.");
  renderAll();
});

elements.maintenanceList.addEventListener("click", (event) => {
  const button = event.target.closest("[data-maintenance-action]");
  if (!button) return;
  const task = state.maintenance.find((item) => item.id === Number(button.dataset.id));
  if (!task) return;
  task.status = button.dataset.maintenanceAction === "start" ? "InProgress" : "Completed";
  pushActivity(`Maintenance #${task.id} changed to ${task.status}.`);
  showToast("Maintenance status updated.");
  renderAll();
});

elements.ticketFilter.addEventListener("change", renderTickets);

document.querySelector("#resetDemo").addEventListener("click", () => {
  state = clone(initialState);
  showToast("Demo state reset.");
  renderAll();
});

document.querySelector("#exportJson").addEventListener("click", () => {
  const payload = JSON.stringify(state, null, 2);
  navigator.clipboard?.writeText(payload);
  showToast("Current dashboard JSON copied to clipboard.");
});

renderAll();

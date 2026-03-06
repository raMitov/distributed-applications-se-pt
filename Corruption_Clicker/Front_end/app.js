const API_BASE = "http://localhost:5059";

let userRole = localStorage.getItem("cc_role") || "";
let token = localStorage.getItem("cc_token") || "";
let userName = localStorage.getItem("cc_user") || "";

const MR_CASH_NORMAL = "images/mr_cash.png";
const MR_CASH_CLICKED = "images/mr_cash_smiling.png";

function showScreen(id) {
  ["screen-loading", "screen-auth", "screen-game"].forEach(s => {
    const el = document.getElementById(s);
    if (el) el.classList.add("hidden");
  });

  const target = document.getElementById(id);
  if (target) target.classList.remove("hidden");
}

function setAuthError(msg) {
  const el = document.getElementById("auth-error");
  if (el) el.textContent = msg || "";
}

function setRegError(msg) {
  const el = document.getElementById("reg-error");
  if (el) el.textContent = msg || "";
}

function setGameError(msg) {
  const el = document.getElementById("game-error");
  if (el) el.textContent = msg || "";
}

async function api(path, options = {}) {
  const headers = options.headers || {};
  if (token) headers["Authorization"] = "Bearer " + token;
  options.headers = headers;

  const res = await fetch(API_BASE + path, options);
  const text = await res.text();

  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {}

  if (!res.ok) {
    const msg = (data && (data.message || data.error)) || text || `HTTP ${res.status}`;
    throw new Error(msg);
  }

  return data;
}

async function login() {
  setAuthError("");

  const u = document.getElementById("login-username").value.trim();
  const p = document.getElementById("login-password").value;

  const data = await api("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userName: u, password: p })
  });

  token = data.token;
  userName = data.user.userName;
  userRole = data.user.role;

  localStorage.setItem("cc_role", userRole);
  localStorage.setItem("cc_token", token);
  localStorage.setItem("cc_user", userName);

  await loadGameState();
  showScreen("screen-game");
}

async function register() {
  setRegError("");

  const u = document.getElementById("reg-username").value.trim();
  const e = document.getElementById("reg-email").value.trim();
  const p = document.getElementById("reg-password").value;

  await api("/api/auth/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userName: u, email: e, password: p })
  });

  document.getElementById("login-username").value = u;
  document.getElementById("login-password").value = p;
}

function logout() {
  userRole = "";
  token = "";
  userName = "";

  localStorage.removeItem("cc_role");
  localStorage.removeItem("cc_token");
  localStorage.removeItem("cc_user");

  const btnAdminToggle = document.getElementById("btn-admin-toggle");
  const adminPanel = document.getElementById("admin-panel");

  if (btnAdminToggle) btnAdminToggle.classList.add("hidden");
  if (adminPanel) adminPanel.classList.add("hidden");

  showScreen("screen-auth");
}

function normalizeImageUrl(path) {
  if (!path) return "images/upgrades/placeholder.png";
  if (path.startsWith("http")) return path;
  if (path.startsWith("/")) return path.substring(1);
  return path;
}

function renderUpgrades(list) {
  const container = document.getElementById("upgrades-list");
  if (!container) return;

  container.innerHTML = "";

  if (!Array.isArray(list)) {
    console.log("renderUpgrades received:", list);
    return;
  }

  list.forEach(u => {
    const div = document.createElement("div");
    div.className = "upgrade";

    div.innerHTML = `
      <img src="${normalizeImageUrl(u.imageUrl)}" alt="${u.name}">
      <div class="meta">
        <div class="name">${u.name} (x${u.quantity})</div>
        <div class="sub">Cost: ${u.nextCost} | +CPS ${u.cpsBonus} | +CPC ${u.cpcBonus}</div>
      </div>
      <button data-id="${u.upgradeId}">Buy</button>
    `;

    div.querySelector("button").onclick = async () => {
      try {
        setGameError("");
        await api(`/api/game/buy/${u.upgradeId}`, { method: "POST" });
        await loadGameState();
      } catch (err) {
        setGameError(err.message);
      }
    };

    container.appendChild(div);
  });
}

async function loadGameState() {
  const data = await api("/api/game/state");

  const txtUser = document.getElementById("txt-user");
  const txtBalance = document.getElementById("txt-balance");
  const txtCpc = document.getElementById("txt-cpc");
  const btnAdminToggle = document.getElementById("btn-admin-toggle");

  if (txtUser) txtUser.textContent = `User: ${userName}`;
  if (txtBalance) txtBalance.textContent = `Cash: ${data.cashBalance}`;
  if (txtCpc) txtCpc.textContent = `Cash/Click: ${data.cashPerClick}`;

  if (btnAdminToggle) {
    if (userRole === "Admin") btnAdminToggle.classList.remove("hidden");
    else btnAdminToggle.classList.add("hidden");
  }

  renderUpgrades(data.upgrades);
}

let cashAnimating = false;

async function click_Mr_Cash() {
  try {
    setGameError("");

    const mrCash = document.getElementById("mr_cash");
    if (!mrCash) return;

    if (!cashAnimating) {
      cashAnimating = true;
      mrCash.src = MR_CASH_CLICKED;
      mrCash.classList.add("clicked");

      setTimeout(() => {
        mrCash.src = MR_CASH_NORMAL;
        mrCash.classList.remove("clicked");
        cashAnimating = false;
      }, 140);
    }

    await api("/api/game/click", { method: "POST" });
    await loadGameState();
  } catch (err) {
    setGameError(err.message);
  }
}

// ---------- ADMIN ---------- 

function showAdminTab(id) {
  ["admin-upgrades", "admin-users", "admin-userupgrades"].forEach(x => {
    const el = document.getElementById(x);
    if (el) el.classList.add("hidden");
  });

  const target = document.getElementById(id);
  if (target) target.classList.remove("hidden");
}

function fillUpgradeForm(u) {
  document.getElementById("upgrade-id").value = u.upgradeId;
  document.getElementById("upgrade-name").value = u.name;
  document.getElementById("upgrade-description").value = u.description;
  document.getElementById("upgrade-imageurl").value = u.imageUrl;
  document.getElementById("upgrade-basecost").value = u.baseCost;
  document.getElementById("upgrade-cpsbonus").value = u.cpsBonus;
  document.getElementById("upgrade-cpcbonus").value = u.cpcBonus;
  document.getElementById("upgrade-maxquantity").value = u.maxQuantity;
  document.getElementById("upgrade-isactive").checked = u.isActive;
}

function clearUpgradeForm() {
  document.getElementById("upgrade-id").value = "";
  document.getElementById("upgrade-name").value = "";
  document.getElementById("upgrade-description").value = "";
  document.getElementById("upgrade-imageurl").value = "";
  document.getElementById("upgrade-basecost").value = "";
  document.getElementById("upgrade-cpsbonus").value = "";
  document.getElementById("upgrade-cpcbonus").value = "";
  document.getElementById("upgrade-maxquantity").value = "";
  document.getElementById("upgrade-isactive").checked = true;
}

async function loadAdminUpgrades() {
  const data = await api("/api/upgrades?page=1&pageSize=100");
  const list = data.items || [];
  const container = document.getElementById("admin-upgrades-list");
  if (!container) return;

  container.innerHTML = "";

  list.forEach(u => {
    const div = document.createElement("div");
    div.className = "admin-row";
    div.innerHTML = `
      <div>
        <strong>${u.name}</strong><br>
        <small>ID: ${u.upgradeId} | Cost: ${u.baseCost} | CPS: ${u.cpsBonus} | CPC: ${u.cpcBonus}</small>
      </div>
      <div class="actions">
        <button>Edit</button>
        <button>Delete</button>
      </div>
    `;

    const buttons = div.querySelectorAll("button");
    buttons[0].onclick = () => fillUpgradeForm(u);
    buttons[1].onclick = async () => {
      if (!confirm(`Delete upgrade "${u.name}"?`)) return;
      await api(`/api/upgrades/${u.upgradeId}`, { method: "DELETE" });
      await loadAdminUpgrades();
      await loadGameState();
    };

    container.appendChild(div);
  });
}

async function loadAdminUsers() {
  const data = await api("/api/users?page=1&pageSize=50");
  const container = document.getElementById("admin-users-list");
  if (!container) return;

  container.innerHTML = "";

  (data.items || []).forEach(u => {
    const div = document.createElement("div");
    div.className = "admin-row";

    div.innerHTML = `
      <div>
        <strong>${u.userName}</strong><br>
        <small>
          ID:${u.userId} |
          Email:${u.email} |
          Role:${u.role} |
          Balance:${u.cashBalance}
        </small>
      </div>

      <div class="actions">
        <button>Edit</button>
        <button>Delete</button>
      </div>
    `;

    const buttons = div.querySelectorAll("button");

    buttons[0].onclick = async () => {
      const name = prompt("Username:", u.userName);
      const email = prompt("Email:", u.email);
      const role = prompt("Role:", u.role);

      if (!name || !email || !role) return;

      await api(`/api/users/${u.userId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          userName: name,
          email: email,
          role: role
        })
      });

      await loadAdminUsers();
    };

    buttons[1].onclick = async () => {
      if (!confirm("Delete user?")) return;

      await api(`/api/users/${u.userId}`, {
        method: "DELETE"
      });

      await loadAdminUsers();
    };

    container.appendChild(div);
  });
}

async function loadAdminUserUpgrades() {
  const data = await api("/api/userupgrades?page=1&pageSize=100");
  const container = document.getElementById("admin-userupgrades-list");
  if (!container) return;

  container.innerHTML = "";

  (data.items || []).forEach(x => {
    const div = document.createElement("div");
    div.className = "admin-row";

    div.innerHTML = `
      <div>
        <strong>User ${x.userId}</strong><br>
        <small>
          Upgrade:${x.upgradeId} |
          Quantity:${x.quantity} |
          Spent:${x.totalSpent}
        </small>
      </div>

      <div class="actions">
        <button>Edit</button>
        <button>Delete</button>
      </div>
    `;

    const buttons = div.querySelectorAll("button");

    buttons[0].onclick = async () => {
      const quantity = prompt("Quantity:", x.quantity);
      const spent = prompt("TotalSpent:", x.totalSpent);

      if (!quantity || !spent) return;

      await api(`/api/userupgrades/${x.userUpgradeId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          userId: x.userId,
          upgradeId: x.upgradeId,
          quantity: Number(quantity),
          totalSpent: Number(spent),
          isEquipped: true
        })
      });

      await loadAdminUserUpgrades();
    };

    buttons[1].onclick = async () => {
      if (!confirm("Delete upgrade?")) return;

      await api(`/api/userupgrades/${x.userUpgradeId}`, {
        method: "DELETE"
      });

      await loadAdminUserUpgrades();
    };

    container.appendChild(div);
  });
}

function wireAdminPanel() {
  const btnAdminToggle = document.getElementById("btn-admin-toggle");
  const btnAdminClose = document.getElementById("btn-admin-close");
  const tabUpgrades = document.getElementById("tab-upgrades");
  const tabUsers = document.getElementById("tab-users");
  const tabUserUpgrades = document.getElementById("tab-userupgrades");
  const btnUpgradeClear = document.getElementById("btn-upgrade-clear");
  const btnUpgradeSave = document.getElementById("btn-upgrade-save");

  if (btnAdminToggle) {
    btnAdminToggle.onclick = async () => {
      const adminPanel = document.getElementById("admin-panel");
      if (!adminPanel) return;

      adminPanel.classList.toggle("hidden");

      if (!adminPanel.classList.contains("hidden")) {
        showAdminTab("admin-upgrades");
        await loadAdminUpgrades();
        await loadAdminUsers();
        await loadAdminUserUpgrades();
      }
    };
  }

  if (btnAdminClose) {
    btnAdminClose.onclick = () => {
      const adminPanel = document.getElementById("admin-panel");
      if (adminPanel) adminPanel.classList.add("hidden");
    };
  }

  if (tabUpgrades) {
    tabUpgrades.onclick = () => {
      showAdminTab("admin-upgrades");
      loadAdminUpgrades();
    };
  }

  if (tabUsers) {
    tabUsers.onclick = () => {
      showAdminTab("admin-users");
      loadAdminUsers();
    };
  }

  if (tabUserUpgrades) {
    tabUserUpgrades.onclick = () => {
      showAdminTab("admin-userupgrades");
      loadAdminUserUpgrades();
    };
  }

  if (btnUpgradeClear) {
    btnUpgradeClear.onclick = clearUpgradeForm;
  }

  if (btnUpgradeSave) {
    btnUpgradeSave.onclick = async () => {
      const id = document.getElementById("upgrade-id").value.trim();

      const payload = {
        name: document.getElementById("upgrade-name").value.trim(),
        description: document.getElementById("upgrade-description").value.trim(),
        imageUrl: document.getElementById("upgrade-imageurl").value.trim(),
        baseCost: Number(document.getElementById("upgrade-basecost").value),
        cpsBonus: Number(document.getElementById("upgrade-cpsbonus").value),
        cpcBonus: Number(document.getElementById("upgrade-cpcbonus").value),
        maxQuantity: Number(document.getElementById("upgrade-maxquantity").value),
        isActive: document.getElementById("upgrade-isactive").checked
      };

      if (id) {
        await api(`/api/upgrades/${id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });
      } else {
        await api("/api/upgrades", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });
      }

      clearUpgradeForm();
      await loadAdminUpgrades();
      await loadGameState();
    };
  }
}

//---------- STARTUP ---------

function wireMainButtons() {
  const btnStart = document.getElementById("btn-start");
  const btnLogin = document.getElementById("btn-login");
  const btnRegister = document.getElementById("btn-register");
  const btnLogout = document.getElementById("btn-logout");
  const mrCash = document.getElementById("mr_cash");

  if (btnStart) {
    btnStart.onclick = () => {
      if (token) {
        loadGameState()
          .then(() => showScreen("screen-game"))
          .catch(() => showScreen("screen-auth"));
      } else {
        showScreen("screen-auth");
      }
    };
  }

  if (btnLogin) {
    btnLogin.onclick = () => login().catch(e => setAuthError(e.message));
  }

  if (btnRegister) {
    btnRegister.onclick = () => register().catch(e => setRegError(e.message));
  }

  if (btnLogout) {
    btnLogout.onclick = logout;
  }

  if (mrCash) {
    mrCash.onclick = click_Mr_Cash;
  }
}

function startTickLoop() {
  setInterval(async () => {
    if (!token) return;
    try {
      await api("/api/game/tick", { method: "POST" });
      await loadGameState();
    } catch {}
  }, 1000);
}

document.addEventListener("DOMContentLoaded", () => {
  wireMainButtons();
  wireAdminPanel();
  startTickLoop();
  showScreen("screen-loading");
});
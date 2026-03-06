// CHANGE THIS if your API port differs
const API_BASE = "http://localhost:5059";

let token = localStorage.getItem("cc_token") || "";
let userName = localStorage.getItem("cc_user") || "";

function showScreen(id){
  ["screen-loading","screen-auth","screen-game"].forEach(s =>
    document.getElementById(s).classList.add("hidden")
  );
  document.getElementById(id).classList.remove("hidden");
}

function setAuthError(msg){
  document.getElementById("auth-error").textContent = msg || "";
}
function setRegError(msg){
  document.getElementById("reg-error").textContent = msg || "";
}
function setGameError(msg){
  document.getElementById("game-error").textContent = msg || "";
}

async function api(path, options = {}){
  const headers = options.headers || {};
  if (token) headers["Authorization"] = "Bearer " + token;
  options.headers = headers;

  const res = await fetch(API_BASE + path, options);
  const text = await res.text();

  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { /* ignore */ }

  if (!res.ok){
    const msg = (data && (data.message || data.error)) || text || `HTTP ${res.status}`;
    throw new Error(msg);
  }
  return data;
}

async function login(){
  setAuthError("");
  const u = document.getElementById("login-username").value.trim();
  const p = document.getElementById("login-password").value;

  const data = await api("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type":"application/json" },
    body: JSON.stringify({ userName: u, password: p })
  });

  token = data.token;
  userName = data.user.userName;

  localStorage.setItem("cc_token", token);
  localStorage.setItem("cc_user", userName);

  await loadGameState();
  showScreen("screen-game");
}

async function register(){
  setRegError("");
  const u = document.getElementById("reg-username").value.trim();
  const e = document.getElementById("reg-email").value.trim();
  const p = document.getElementById("reg-password").value;

  await api("/api/auth/register", {
    method: "POST",
    headers: { "Content-Type":"application/json" },
    body: JSON.stringify({ userName: u, email: e, password: p })
  });

  // after register, user logs in
  document.getElementById("login-username").value = u;
  document.getElementById("login-password").value = p;
}

function logout(){
  token = "";
  userName = "";
  localStorage.removeItem("cc_token");
  localStorage.removeItem("cc_user");
  showScreen("screen-auth");
}

function renderUpgrades(list){
  const container = document.getElementById("upgrades-list");
  container.innerHTML = "";

  list.forEach(u => {
    const div = document.createElement("div");
    div.className = "upgrade";

    div.innerHTML = `
      <img src="${u.imageUrl}" alt="">
      <div class="meta">
        <div class="name">${u.name} (x${u.quantity})</div>
        <div class="sub">Cost: ${u.nextCost} | +CPS ${u.cpsBonus} | +CPC ${u.cpcBonus}</div>
      </div>
      <button data-id="${u.upgradeId}">Buy</button>
    `;

    div.querySelector("button").onclick = async () => {
      try{
        setGameError("");
        await api(`/api/game/buy/${u.upgradeId}`, { method: "POST" });
        await loadGameState();
      }catch(err){
        setGameError(err.message);
      }
    };

    container.appendChild(div);
  });
}

async function loadGameState(){
  const data = await api("/api/game/state");
  document.getElementById("txt-user").textContent = `User: ${userName}`;
  document.getElementById("txt-balance").textContent = `Cash: ${data.cashBalance}`;
  document.getElementById("txt-cpc").textContent = `Cash/Click: ${data.cashPerClick}`;
  renderUpgrades(data.upgrades);
}

async function click_Mr_Cash(){
  try{
    setGameError("");
    await api("/api/game/click", { method: "POST" });
    await loadGameState();
  }catch(err){
    setGameError(err.message);
  }
}

// Wire buttons
document.getElementById("btn-start").onclick = () => {
  if (token) {
    loadGameState().then(() => showScreen("screen-game")).catch(() => showScreen("screen-auth"));
  } else {
    showScreen("screen-auth");
  }
};
setInterval(async () => {
  if (!token) return;
  try {
    await api("/api/game/tick", { method: "POST" });
    await loadGameState();
  } catch {
  }
}, 1000);
document.getElementById("btn-login").onclick = () => login().catch(e => setAuthError(e.message));
document.getElementById("btn-register").onclick = () => register().catch(e => setRegError(e.message));
document.getElementById("btn-logout").onclick = logout;

document.getElementById("mr_cash").onclick = click_Mr_Cash;
document.getElementById("btn-click").onclick = click_Mr_Cash;

// Initial screen
showScreen("screen-loading");
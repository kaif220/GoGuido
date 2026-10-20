const API = '/api/v1';
const S = { token: localStorage.gg_token, refresh: localStorage.gg_refresh, user: JSON.parse(localStorage.gg_user || 'null'), cityId: 0, cities: [], monuments: [] };
const $ = id => document.getElementById(id);
const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const inr = n => '₹' + Number(n).toLocaleString('en-IN');
const go = id => $(id).scrollIntoView({ behavior: 'smooth' });

function toast(msg, err) { const t = $('toast'); t.textContent = msg; t.className = 'toast show' + (err ? ' err' : ''); setTimeout(() => t.className = 'toast', 2800); }
function openModal(html) { $('modalBody').innerHTML = html; $('modal').classList.remove('hidden'); }
function closeModal() { $('modal').classList.add('hidden'); }
$('modal').addEventListener('click', e => { if (e.target.id === 'modal') closeModal(); });

async function api(path, { method = 'GET', body } = {}, retry = true) {
  const h = { 'Content-Type': 'application/json' };
  if (S.token) h.Authorization = 'Bearer ' + S.token;
  const r = await fetch(API + path, { method, headers: h, body: body ? JSON.stringify(body) : undefined });
  if (r.status === 401 && S.refresh && retry && await doRefresh()) return api(path, { method, body }, false);
  const t = await r.text(); let d = null; try { d = t ? JSON.parse(t) : null; } catch { }
  if (!r.ok) { if (r.status === 401) logout(true); throw new Error(d?.message || d?.title || 'Something went wrong'); }
  return d;
}
async function doRefresh() {
  try {
    const r = await fetch(API + '/auth/refresh', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ refreshToken: S.refresh }) });
    if (!r.ok) return false; saveAuth(await r.json()); return true;
  } catch { return false; }
}
function saveAuth(a) {
  S.token = a.accessToken; S.refresh = a.refreshToken; S.user = a.user;
  localStorage.gg_token = S.token; localStorage.gg_refresh = S.refresh; localStorage.gg_user = JSON.stringify(S.user);
}
function logout(silent) { S.token = S.refresh = S.user = null; localStorage.clear(); renderNav(); show('home'); if (!silent) toast('Signed out'); }

// ---------- nav / views ----------
function renderNav() {
  const u = S.user; let h = '';
  if (u) {
    if (u.role === 'Tourist') h += `<button class="nl" onclick="show('trips')">My Trips</button>`;
    if (u.role === 'Guide') h += `<button class="nl" onclick="show('guide')">Guide Dashboard</button>`;
    if (u.role === 'Admin') h += `<button class="nl" onclick="show('admin')">Admin</button>`;
    h += `<span class="tag">${esc(u.fullName)} · ${u.role}</span><button class="btn ghost sm" onclick="logout()">Sign out</button>`;
  } else h = `<button class="btn gold sm" onclick="authModal('login')">Sign in</button>`;
  $('navLinks').innerHTML = h;
}
function show(v) {
  document.querySelectorAll('.view').forEach(x => x.classList.add('hidden'));
  $('view-' + v).classList.remove('hidden'); window.scrollTo(0, 0);
  if (v === 'trips') loadTrips('tripList'); if (v === 'guide') loadGuideDash(); if (v === 'admin') loadAdmin();
}

// ---------- auth ----------
function authModal(mode = 'login') {
  const reg = mode === 'register';
  openModal(`<h3>${reg ? 'Create account' : 'Welcome back'}</h3>
  <div class="tabs"><button class="chip ${!reg ? 'on' : ''}" onclick="authModal('login')">Sign in</button><button class="chip ${reg ? 'on' : ''}" onclick="authModal('register')">Register</button></div>
  <div class="form">
    ${reg ? `<label>Full name<input id="aName"></label><label>I am a<select id="aRole"><option>Tourist</option><option>Guide</option></select></label>` : ''}
    <label>Email<input id="aEmail" type="email"></label>
    <label>Password<input id="aPass" type="password"></label>
    <button class="btn gold" onclick="doAuth('${mode}')">${reg ? 'Create account' : 'Sign in'}</button>
  </div>
  ${reg ? '' : `<div class="demo"><b>Demo logins</b><br>Tourist: tourist@goguido.in / Tourist@123<br>Guide: rajesh@goguido.in / Guide@123<br>Admin: admin@goguido.in / Admin@123</div>`}`);
}
async function doAuth(mode) {
  try {
    const body = { email: $('aEmail').value, password: $('aPass').value };
    if (mode === 'register') { body.fullName = $('aName').value; body.role = $('aRole').value; }
    saveAuth(await api('/auth/' + mode, { method: 'POST', body })); closeModal(); renderNav();
    toast('Welcome, ' + S.user.fullName.split(' ')[0] + '!');
    if (S.user.role === 'Guide') show('guide'); else if (S.user.role === 'Admin') show('admin'); else show('home');
  } catch (e) { toast(e.message, true); }
}

// ---------- home: stats / cities / monuments / guides ----------
async function init() {
  renderNav();
  const [st, cities] = await Promise.all([api('/stats/public'), api('/cities')]);
  S.cities = cities;
  $('stats').innerHTML = [['cities', 'Cities'], ['monuments', 'Monuments'], ['guides', 'Verified guides'], ['trips', 'Trips completed']]
    .map(([k, l]) => `<div class="stat"><b>${st[k] ?? 0}</b><span>${l}</span></div>`).join('');
  renderChips();
  $('gCity').innerHTML = `<option value="0">All cities</option>` + cities.map(c => `<option value="${c.id}">${esc(c.name)}</option>`).join('');
  loadMonuments(); loadGuides();
}
function renderChips() {
  $('cityChips').innerHTML = `<button class="chip ${!S.cityId ? 'on' : ''}" onclick="pickCity(0)">All India</button>` +
    S.cities.map(c => `<button class="chip ${S.cityId === c.id ? 'on' : ''}" onclick="pickCity(${c.id})">${c.icon} ${esc(c.name)}</button>`).join('');
}
function pickCity(id) { S.cityId = id; renderChips(); $('gCity').value = id; loadMonuments(); loadGuides(); }
async function loadMonuments() {
  const q = new URLSearchParams({ cityId: S.cityId, q: $('mSearch').value, unesco: $('mUnesco').checked });
  const list = S.monuments = await api('/monuments?' + q);
  $('monuments').innerHTML = list.length ? list.map(m => `
    <article class="mcard">
      <div class="mart" style="background:linear-gradient(135deg,hsl(${m.cityHue},75%,48%),hsl(${(m.cityHue + 40) % 360},55%,20%))">
        <span class="badge">${esc(m.cityName)}</span>${m.isUnesco ? '<span class="badge u">★ UNESCO</span>' : ''}<span>${m.icon}</span></div>
      <div class="mbody"><h3>${esc(m.name)}</h3><div class="meta">${esc(m.era)}</div><p>${esc(m.description)}</p>
        <div class="row"><span class="fee">${m.entryFeeInr > 0 ? 'Entry ≈ ' + inr(m.entryFeeInr) : 'Free entry'}</span>
        <button class="btn gold sm" onclick="pickCity(${m.cityId});go('guides')">Find guides</button></div></div>
    </article>`).join('') : '<div class="empty">No monuments match your search.</div>';
}
const stars = r => '★'.repeat(Math.round(r)) + '☆'.repeat(5 - Math.round(r));
async function loadGuides() {
  S.cityId = +$('gCity').value; renderChips();
  const q = new URLSearchParams({ cityId: S.cityId, language: $('gLang').value, sort: $('gSort').value });
  const list = await api('/guides?' + q);
  $('guideList').innerHTML = list.length ? list.map(g => `
    <div class="gcard">
      <div class="gtop"><div class="av">${esc(g.name.split(' ').map(w => w[0]).slice(0, 2).join(''))}</div>
        <div><b>${esc(g.name)}</b> <span title="KYC verified">✅</span><br><small>${esc(g.cityName)} · ${g.experienceYears} yrs</small><br>
        <span class="stars">${stars(g.rating)}</span> <small>${g.rating ? g.rating.toFixed(1) : 'New'} (${g.reviewCount})</small></div></div>
      <p style="color:var(--mute);font-size:.9rem">${esc(g.bio)}</p>
      <div class="tags">${g.languages.split(',').map(l => `<span class="tag">${esc(l.trim())}</span>`).join('')}</div>
      <div class="row"><div class="price">${inr(g.pricePerHour)}<small> / hour</small></div><button class="btn gold sm" onclick="hireModal(${g.id})">Hire guide</button></div>
    </div>`).join('') : '<div class="empty">No verified guides for this filter yet.</div>';
  window._guides = list;
}

// ---------- hire / trips ----------
function hireModal(id) {
  if (!S.user) return authModal('login');
  if (S.user.role !== 'Tourist') return toast('Only tourist accounts can hire guides', true);
  const g = window._guides.find(x => x.id === id);
  const ms = S.monuments.filter(m => m.cityId === g.cityId);
  const tmr = new Date(Date.now() + 864e5).toISOString().slice(0, 16);
  openModal(`<h3>Hire ${esc(g.name)}</h3><div class="form">
    <label>Date &amp; time<input type="datetime-local" id="hDate" min="${tmr}" value="${tmr}"></label>
    <div class="two"><label>Hours<input type="number" id="hHours" min="1" max="12" value="4" oninput="calc(${g.pricePerHour})"></label>
    <label>Monument<select id="hMon"><option value="">Any / custom</option>${ms.map(m => `<option value="${m.id}">${esc(m.name)}</option>`).join('')}</select></label></div>
    <label>Notes<textarea id="hNotes" rows="2" placeholder="Group size, interests, pickup point…"></textarea></label>
    <div class="price" id="hTotal">${inr(g.pricePerHour * 4)}</div>
    <button class="btn gold" onclick="hire(${g.id})">Send request</button></div>`);
}
const calc = p => $('hTotal').textContent = inr(p * (+$('hHours').value || 0));
async function hire(gid) {
  try {
    await api('/bookings', { method: 'POST', body: { guideId: gid, monumentId: $('hMon').value ? +$('hMon').value : null, tourDate: new Date($('hDate').value).toISOString(), hours: +$('hHours').value, notes: $('hNotes').value } });
    closeModal(); toast('Request sent! The guide will respond soon.'); show('trips');
  } catch (e) { toast(e.message, true); }
}
function tripCard(b, role) {
  const a = [];
  if (role === 'Guide') {
    if (b.status === 'Pending') a.push(btn('Accept', b.id, 'Accepted', 'ok'), btn('Decline', b.id, 'Declined', 'bad'));
    if (b.status === 'Accepted') a.push(btn('Start tour', b.id, 'InProgress', 'gold'));
    if (b.status === 'InProgress') a.push(btn('Mark completed', b.id, 'Completed', 'ok'));
  } else {
    if (['Pending', 'Accepted'].includes(b.status)) a.push(btn('Cancel', b.id, 'Cancelled', 'bad'));
    if (b.status === 'Completed' && !b.reviewed) a.push(`<button class="btn gold sm" onclick="rateModal(${b.id})">Rate guide</button>`);
  }
  return `<div class="trip"><div><h4>${esc(role === 'Guide' ? b.touristName : b.guideName)} <span class="st ${b.status}">${b.status}</span></h4>
    <p>${esc(b.cityName)}${b.monumentName ? ' · ' + esc(b.monumentName) : ''} · ${new Date(b.tourDate).toLocaleString('en-IN', { dateStyle: 'medium', timeStyle: 'short' })} · ${b.hours}h · <b>${inr(b.totalPrice)}</b></p>
    ${b.notes ? `<p>“${esc(b.notes)}”</p>` : ''}</div><div class="actions">${a.join('')}</div></div>`;
}
const btn = (t, id, st, c) => `<button class="btn ${c} sm" onclick="setStatus(${id},'${st}')">${t}</button>`;
async function loadTrips(target) {
  const list = await api('/bookings/mine');
  $(target).innerHTML = list.length ? list.map(b => tripCard(b, S.user.role)).join('') : '<div class="empty">Nothing here yet. Explore monuments and hire a guide!</div>';
}
async function setStatus(id, status) {
  try { await api(`/bookings/${id}/status`, { method: 'PUT', body: { status } }); toast('Updated to ' + status); S.user.role === 'Guide' ? loadTrips('guideTrips') : loadTrips('tripList'); }
  catch (e) { toast(e.message, true); }
}
let pick = 5;
function rateModal(id) {
  pick = 5;
  openModal(`<h3>Rate your guide</h3><div class="star-pick" id="sp"></div><div class="form"><label>Review<textarea id="rText" rows="3"></textarea></label><button class="btn gold" onclick="rate(${id})">Submit review</button></div>`);
  drawStars();
}
function drawStars() { $('sp').innerHTML = [1, 2, 3, 4, 5].map(i => `<span class="${i <= pick ? 'on' : ''}" onclick="pick=${i};drawStars()">★</span>`).join(''); }
async function rate(id) { try { await api('/reviews', { method: 'POST', body: { bookingId: id, rating: pick, comment: $('rText').value } }); closeModal(); toast('Thanks for your review!'); loadTrips('tripList'); init(); } catch (e) { toast(e.message, true); } }

// ---------- guide dashboard ----------
async function loadGuideDash() {
  const p = await api('/guides/me') || { cityId: S.cities[0]?.id, bio: '', languages: 'English,Hindi', experienceYears: 1, pricePerHour: 500, licenseNumber: '', status: null };
  $('guideForm').innerHTML = `<div class="row" style="margin:0 0 14px"><h4>My profile</h4>${p.status ? `<span class="st ${p.status}">KYC: ${p.status}</span>` : '<span class="st">Not submitted</span>'}</div>
    ${p.rejectReason ? `<p style="color:var(--rose);margin-bottom:10px">Rejected: ${esc(p.rejectReason)} — update details and resubmit.</p>` : ''}
    ${p.status === 'Pending' ? '<p style="color:var(--gold);margin-bottom:10px">Your profile is awaiting admin verification. It goes live once approved.</p>' : ''}
    <div class="form"><div class="two"><label>City<select id="pCity">${S.cities.map(c => `<option value="${c.id}" ${c.id === p.cityId ? 'selected' : ''}>${esc(c.name)}</option>`).join('')}</select></label>
    <label>Licence number<input id="pLic" value="${esc(p.licenseNumber)}"></label></div>
    <div class="two"><label>Experience (years)<input type="number" id="pExp" value="${p.experienceYears}"></label><label>Price per hour (₹)<input type="number" id="pPrice" value="${p.pricePerHour}"></label></div>
    <label>Languages (comma separated)<input id="pLang" value="${esc(p.languages)}"></label>
    <label>Bio<textarea id="pBio" rows="3">${esc(p.bio)}</textarea></label>
    <button class="btn gold" onclick="saveGuide()">Save &amp; submit for verification</button></div>`;
  loadTrips('guideTrips');
}
async function saveGuide() {
  try {
    await api('/guides/me', { method: 'PUT', body: { cityId: +$('pCity').value, bio: $('pBio').value, languages: $('pLang').value, experienceYears: +$('pExp').value, pricePerHour: +$('pPrice').value, licenseNumber: $('pLic').value } });
    toast('Profile saved'); loadGuideDash();
  } catch (e) { toast(e.message, true); }
}

// ---------- admin ----------
async function loadAdmin() {
  const [s, gl] = await Promise.all([api('/stats/admin'), api('/admin/guides')]);
  const bk = Object.values(s.bookings).reduce((a, b) => a + b, 0);
  $('adminStats').innerHTML = [['Tourists', s.users.Tourist ?? 0], ['Guides', s.users.Guide ?? 0], ['Bookings', bk], ['Pending KYC', s.pendingGuides], ['Revenue', inr(s.revenue)], ['Commission (10%)', inr(s.commission)]]
    .map(([l, v]) => `<div class="stat"><b>${v}</b><span>${l}</span></div>`).join('');
  $('adminGuides').innerHTML = gl.map(g => `<div class="trip"><div><h4>${esc(g.name)} <span class="st ${g.status}">${g.status}</span></h4>
    <p>${esc(g.city)} · Licence ${esc(g.licenseNumber)} · ${g.experienceYears} yrs · ${inr(g.pricePerHour)}/h · ${esc(g.email)}</p></div>
    <div class="actions">${g.status !== 'Approved' ? `<button class="btn ok sm" onclick="verify(${g.id},true)">Approve</button>` : ''}${g.status !== 'Rejected' ? `<button class="btn bad sm" onclick="verify(${g.id},false)">Reject</button>` : ''}</div></div>`).join('');
}
async function verify(id, approve) {
  let reason = null; if (!approve) { reason = prompt('Reason for rejection?'); if (!reason) return; }
  try { await api(`/admin/guides/${id}/verify`, { method: 'PUT', body: { approve, reason } }); toast(approve ? 'Guide approved' : 'Guide rejected'); loadAdmin(); } catch (e) { toast(e.message, true); }
}

init().catch(e => toast('Cannot reach API: ' + e.message, true));

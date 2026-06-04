/**
 * ============================================================
 * TEKNIK DESTEK SISTEMI - ANA UYGULAMA MOTORU
 * JSON tabanlı veri katmanı, Auth, CRUD, Validasyon
 * ============================================================
 */

'use strict';

/* ============================================================
   VERİ KATMANI (JSON Store - LocalStorage tabanlı)
   EF Core Code-First yaklaşımının JS karşılığı:
   - DbContext yerine: DataStore
   - Migrations yerine: initDB()
   - CRUD metodları: getAll, getById, create, update, delete
   ============================================================ */

const DataStore = (() => {
  const DB_KEY = 'ticketSystemDB';

  // Seed Data (İlk çalıştırmada yüklenir)
  const seedData = {
    users: [
      { id: 1, username: 'admin', email: 'admin@destek.com', password: 'Admin123!', fullName: 'Sistem Yöneticisi', role: 'admin', createdAt: '2024-01-01T00:00:00.000Z', isActive: true },
      { id: 2, username: 'support1', email: 'destek@destek.com', password: 'Support123!', fullName: 'Ahmet Yılmaz', role: 'support', createdAt: '2024-01-05T00:00:00.000Z', isActive: true },
      { id: 3, username: 'user1', email: 'kullanici@ornek.com', password: 'User123!', fullName: 'Mehmet Kaya', role: 'user', createdAt: '2024-02-10T00:00:00.000Z', isActive: true },
      { id: 4, username: 'user2', email: 'ayse@ornek.com', password: 'User123!', fullName: 'Ayşe Demir', role: 'user', createdAt: '2024-02-15T00:00:00.000Z', isActive: true }
    ],
    tickets: [
      { id: 1, ticketNo: 'TKT-0001', title: 'Giriş yapamıyorum, şifrem çalışmıyor', description: 'Sabahtan beri sisteme giriş yapamıyorum. Şifremi sıfırlamayı denedim ama e-posta gelmiyor. Acil yardım lazım.', category: 'Hesap', priority: 'critical', status: 'open', userId: 3, assignedTo: null, createdAt: '2024-11-01T08:30:00.000Z', updatedAt: '2024-11-01T08:30:00.000Z', comments: [{ id: 1, userId: 3, text: 'Hala çözüm beklemekteyim, acil durum.', createdAt: '2024-11-01T09:00:00.000Z' }] },
      { id: 2, ticketNo: 'TKT-0002', title: 'Fatura indirme özelliği çalışmıyor', description: 'Faturalarım sayfasından PDF indirmeye çalıştığımda hata alıyorum. "Dosya bulunamadı" hatası veriyor.', category: 'Teknik', priority: 'high', status: 'in-progress', userId: 4, assignedTo: 2, createdAt: '2024-11-02T10:15:00.000Z', updatedAt: '2024-11-02T14:20:00.000Z', comments: [{ id: 2, userId: 2, text: 'Merhaba, sorununuzu inceliyoruz. Sunucu tarafında bir sorun tespit ettik, en kısa sürede çözülecek.', createdAt: '2024-11-02T14:20:00.000Z' }] },
      { id: 3, ticketNo: 'TKT-0003', title: 'Abonelik planımı yükseltmek istiyorum', description: 'Mevcut planımı Pro plana yükseltmek istiyorum ancak ödeme sayfasında hata alıyorum.', category: 'Faturalama', priority: 'medium', status: 'resolved', userId: 3, assignedTo: 2, createdAt: '2024-10-28T11:00:00.000Z', updatedAt: '2024-10-29T16:00:00.000Z', comments: [{ id: 3, userId: 2, text: 'Ödeme sayfasındaki sorun giderildi. Şimdi deneyebilirsiniz.', createdAt: '2024-10-29T15:30:00.000Z' }, { id: 4, userId: 3, text: 'Teşekkürler, sorun çözüldü. Plan yükseltmemi tamamladım.', createdAt: '2024-10-29T16:00:00.000Z' }] },
      { id: 4, ticketNo: 'TKT-0004', title: 'Mobil uygulamada bildirimler gelmiyor', description: 'iOS uygulamasında push bildirimleri almıyorum. Ayarları kontrol ettim, her şey açık görünüyor.', category: 'Mobil', priority: 'low', status: 'closed', userId: 4, assignedTo: 2, createdAt: '2024-10-20T09:00:00.000Z', updatedAt: '2024-10-22T11:00:00.000Z', comments: [{ id: 5, userId: 2, text: 'iOS 17 güncellemesiyle gelen bir uyumsuzluktu. Uygulama güncellemesi yayınlandı, lütfen güncelleyin.', createdAt: '2024-10-22T10:00:00.000Z' }, { id: 6, userId: 4, text: 'Güncelleme sonrası düzeldi, teşekkürler!', createdAt: '2024-10-22T11:00:00.000Z' }] },
      { id: 5, ticketNo: 'TKT-0005', title: 'Raporlama modülünde veri eksikliği', description: 'Aylık raporlarda son 3 günün verisi görünmüyor. Grafikler de yanlış gösteriyor.', category: 'Teknik', priority: 'high', status: 'open', userId: 3, assignedTo: null, createdAt: '2024-11-03T07:45:00.000Z', updatedAt: '2024-11-03T07:45:00.000Z', comments: [] }
    ],
    categories: ['Teknik', 'Hesap', 'Faturalama', 'Mobil', 'Genel', 'Diğer'],
    nextUserId: 5,
    nextTicketId: 6,
    nextCommentId: 7
  };

  // DB Başlatma (Migration benzeri)
  function initDB() {
    if (!localStorage.getItem(DB_KEY)) {
      localStorage.setItem(DB_KEY, JSON.stringify(seedData));
    }
  }

  // DB Okuma
  function getDB() {
    return JSON.parse(localStorage.getItem(DB_KEY));
  }

  // DB Yazma
  function saveDB(db) {
    localStorage.setItem(DB_KEY, JSON.stringify(db));
  }

  // DB Sıfırlama
  function resetDB() {
    localStorage.setItem(DB_KEY, JSON.stringify(seedData));
  }

  /* ---- KULLANICI CRUD ---- */
  const Users = {
    getAll() {
      return getDB().users;
    },
    getById(id) {
      return getDB().users.find(u => u.id === id) || null;
    },
    getByEmail(email) {
      return getDB().users.find(u => u.email.toLowerCase() === email.toLowerCase()) || null;
    },
    getByUsername(username) {
      return getDB().users.find(u => u.username.toLowerCase() === username.toLowerCase()) || null;
    },
    create(data) {
      const db = getDB();
      const user = {
        id: db.nextUserId++,
        username: data.username,
        email: data.email,
        password: data.password,
        fullName: data.fullName,
        role: data.role || 'user',
        createdAt: new Date().toISOString(),
        isActive: true
      };
      db.users.push(user);
      saveDB(db);
      return user;
    },
    update(id, data) {
      const db = getDB();
      const idx = db.users.findIndex(u => u.id === id);
      if (idx === -1) return null;
      db.users[idx] = { ...db.users[idx], ...data };
      saveDB(db);
      return db.users[idx];
    },
    delete(id) {
      const db = getDB();
      const idx = db.users.findIndex(u => u.id === id);
      if (idx === -1) return false;
      db.users.splice(idx, 1);
      saveDB(db);
      return true;
    },
    authenticate(email, password) {
      const user = this.getByEmail(email);
      if (!user) return null;
      if (user.password !== password) return null;
      if (!user.isActive) return null;
      return user;
    }
  };

  /* ---- TİCKET CRUD ---- */
  const Tickets = {
    getAll() {
      return getDB().tickets;
    },
    getById(id) {
      return getDB().tickets.find(t => t.id === id) || null;
    },
    // LINQ benzeri filtreleme
    filter({ status, priority, category, userId, assignedTo, search } = {}) {
      let tickets = this.getAll();
      // Enum tipi: status filtresi
      if (status && status !== 'all') {
        tickets = tickets.filter(t => t.status === status);
      }
      if (priority && priority !== 'all') {
        tickets = tickets.filter(t => t.priority === priority);
      }
      if (category && category !== 'all') {
        tickets = tickets.filter(t => t.category === category);
      }
      if (userId) {
        tickets = tickets.filter(t => t.userId === userId);
      }
      if (assignedTo !== undefined && assignedTo !== null) {
        tickets = tickets.filter(t => t.assignedTo === assignedTo);
      }
      if (search && search.trim()) {
        const q = search.toLowerCase().trim();
        tickets = tickets.filter(t =>
          t.title.toLowerCase().includes(q) ||
          t.ticketNo.toLowerCase().includes(q) ||
          t.description.toLowerCase().includes(q)
        );
      }
      // Tarihe göre azalan sıralama
      return tickets.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
    },
    getOpenTickets() {
      return this.filter({ status: 'open' });
    },
    getInProgressTickets() {
      return this.filter({ status: 'in-progress' });
    },
    create(data) {
      const db = getDB();
      const ticket = {
        id: db.nextTicketId,
        ticketNo: `TKT-${String(db.nextTicketId).padStart(4, '0')}`,
        title: data.title,
        description: data.description,
        category: data.category,
        priority: data.priority || 'medium',
        status: 'open',
        userId: data.userId,
        assignedTo: null,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        comments: []
      };
      db.nextTicketId++;
      db.tickets.push(ticket);
      saveDB(db);
      return ticket;
    },
    update(id, data) {
      const db = getDB();
      const idx = db.tickets.findIndex(t => t.id === id);
      if (idx === -1) return null;
      db.tickets[idx] = { ...db.tickets[idx], ...data, updatedAt: new Date().toISOString() };
      saveDB(db);
      return db.tickets[idx];
    },
    delete(id) {
      const db = getDB();
      const idx = db.tickets.findIndex(t => t.id === id);
      if (idx === -1) return false;
      db.tickets.splice(idx, 1);
      saveDB(db);
      return true;
    },
    addComment(ticketId, userId, text) {
      const db = getDB();
      const ticket = db.tickets.find(t => t.id === ticketId);
      if (!ticket) return null;
      const comment = {
        id: db.nextCommentId++,
        userId,
        text,
        createdAt: new Date().toISOString()
      };
      ticket.comments.push(comment);
      ticket.updatedAt = new Date().toISOString();
      saveDB(db);
      return comment;
    },
    getStats() {
      const all = this.getAll();
      return {
        total: all.length,
        open: all.filter(t => t.status === 'open').length,
        inProgress: all.filter(t => t.status === 'in-progress').length,
        resolved: all.filter(t => t.status === 'resolved').length,
        closed: all.filter(t => t.status === 'closed').length,
        critical: all.filter(t => t.priority === 'critical').length,
        unassigned: all.filter(t => t.assignedTo === null && t.status === 'open').length
      };
    }
  };

  const Categories = {
    getAll() {
      return getDB().categories;
    },
    add(name) {
      const db = getDB();
      if (!db.categories.includes(name)) {
        db.categories.push(name);
        saveDB(db);
      }
    }
  };

  return { initDB, resetDB, Users, Tickets, Categories };
})();

/* ============================================================
   .NET API KATMANI
   - Uygulama HTTP uzerinden calisiyorsa ASP.NET Core API
   - Dosya olarak acildiysa mevcut localStorage modu korunur
   ============================================================ */

const ApiBridge = (() => {
  const BASE_URL = '/api';
  let checked = false;
  let available = false;

  function checkAvailability() {
    if (checked) return available;
    checked = true;

    if (window.location.protocol === 'file:') {
      available = false;
      return available;
    }

    try {
      const xhr = new XMLHttpRequest();
      xhr.open('GET', `${BASE_URL}/health`, false);
      xhr.setRequestHeader('Accept', 'application/json');
      xhr.send(null);
      available = xhr.status >= 200 && xhr.status < 300;
    } catch (err) {
      available = false;
    }

    return available;
  }

  function disable() {
    available = false;
    checked = true;
  }

  function parseResponse(xhr) {
    const raw = xhr.responseText?.trim();
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch (err) {
      return raw;
    }
  }

  function request(method, path, body) {
    const xhr = new XMLHttpRequest();
    xhr.open(method, `${BASE_URL}${path}`, false);
    xhr.setRequestHeader('Accept', 'application/json');
    if (body !== undefined && body !== null) {
      xhr.setRequestHeader('Content-Type', 'application/json');
    }

    xhr.send(body === undefined || body === null ? null : JSON.stringify(body));

    if (xhr.status >= 200 && xhr.status < 300) {
      return parseResponse(xhr);
    }

    const payload = parseResponse(xhr);
    const message = payload?.message || xhr.statusText || `HTTP ${xhr.status}`;
    const error = new Error(message);
    error.status = xhr.status;
    error.payload = payload;
    throw error;
  }

  return { isAvailable: checkAvailability, disable, request };
})();

const LocalDataStore = {
  initDB: DataStore.initDB.bind(DataStore),
  resetDB: DataStore.resetDB.bind(DataStore),
  Users: {
    getAll: DataStore.Users.getAll.bind(DataStore.Users),
    getById: DataStore.Users.getById.bind(DataStore.Users),
    getByEmail: DataStore.Users.getByEmail.bind(DataStore.Users),
    getByUsername: DataStore.Users.getByUsername.bind(DataStore.Users),
    create: DataStore.Users.create.bind(DataStore.Users),
    update: DataStore.Users.update.bind(DataStore.Users),
    delete: DataStore.Users.delete.bind(DataStore.Users),
    authenticate: DataStore.Users.authenticate.bind(DataStore.Users)
  },
  Tickets: {
    getAll: DataStore.Tickets.getAll.bind(DataStore.Tickets),
    getById: DataStore.Tickets.getById.bind(DataStore.Tickets),
    filter: DataStore.Tickets.filter.bind(DataStore.Tickets),
    getOpenTickets: DataStore.Tickets.getOpenTickets.bind(DataStore.Tickets),
    getInProgressTickets: DataStore.Tickets.getInProgressTickets.bind(DataStore.Tickets),
    create: DataStore.Tickets.create.bind(DataStore.Tickets),
    update: DataStore.Tickets.update.bind(DataStore.Tickets),
    delete: DataStore.Tickets.delete.bind(DataStore.Tickets),
    addComment: DataStore.Tickets.addComment.bind(DataStore.Tickets),
    getStats: DataStore.Tickets.getStats.bind(DataStore.Tickets)
  },
  Categories: {
    getAll: DataStore.Categories.getAll.bind(DataStore.Categories),
    add: DataStore.Categories.add.bind(DataStore.Categories)
  }
};

function apiOrLocal(apiFn, localFn, fallbackOnClientError = false) {
  if (!ApiBridge.isAvailable()) {
    return localFn();
  }

  try {
    return apiFn();
  } catch (error) {
    const canFallback =
      error && (typeof error.status !== 'number' || error.status >= 500 || error.status === 0);

    if (canFallback || fallbackOnClientError) {
      ApiBridge.disable();
      return localFn();
    }

    throw error;
  }
}

DataStore.initDB = function () {
  if (ApiBridge.isAvailable()) {
    return true;
  }
  return LocalDataStore.initDB();
};

DataStore.resetDB = function () {
  return apiOrLocal(
    () => ApiBridge.request('POST', '/system/reset'),
    () => LocalDataStore.resetDB(),
    false
  );
};

DataStore.Users.getAll = function () {
  return apiOrLocal(
    () => ApiBridge.request('GET', '/users') || [],
    () => LocalDataStore.Users.getAll()
  );
};

DataStore.Users.getById = function (id) {
  return apiOrLocal(
    () => {
      try {
        return ApiBridge.request('GET', `/users/${id}`);
      } catch (error) {
        if (error.status === 404) return null;
        throw error;
      }
    },
    () => LocalDataStore.Users.getById(id)
  );
};

DataStore.Users.getByEmail = function (email) {
  return apiOrLocal(
    () => {
      try {
        return ApiBridge.request('GET', `/users/by-email?email=${encodeURIComponent(email)}`);
      } catch (error) {
        if (error.status === 404) return null;
        throw error;
      }
    },
    () => LocalDataStore.Users.getByEmail(email)
  );
};

DataStore.Users.getByUsername = function (username) {
  return apiOrLocal(
    () => {
      try {
        return ApiBridge.request('GET', `/users/by-username?username=${encodeURIComponent(username)}`);
      } catch (error) {
        if (error.status === 404) return null;
        throw error;
      }
    },
    () => LocalDataStore.Users.getByUsername(username)
  );
};

DataStore.Users.create = function (data) {
  return apiOrLocal(
    () => ApiBridge.request('POST', '/users', data),
    () => LocalDataStore.Users.create(data),
    false
  );
};

DataStore.Users.update = function (id, data) {
  return apiOrLocal(
    () => ApiBridge.request('PUT', `/users/${id}`, data),
    () => LocalDataStore.Users.update(id, data),
    false
  );
};

DataStore.Users.delete = function (id) {
  return apiOrLocal(
    () => {
      ApiBridge.request('DELETE', `/users/${id}`);
      return true;
    },
    () => LocalDataStore.Users.delete(id),
    false
  );
};

DataStore.Users.authenticate = function (email, password) {
  return apiOrLocal(
    () => {
      try {
        return ApiBridge.request('POST', '/auth/login', { email, password });
      } catch (error) {
        if (error.status === 401) return null;
        throw error;
      }
    },
    () => LocalDataStore.Users.authenticate(email, password)
  );
};

DataStore.Tickets.getAll = function () {
  return apiOrLocal(
    () => ApiBridge.request('GET', '/tickets') || [],
    () => LocalDataStore.Tickets.getAll()
  );
};

DataStore.Tickets.getById = function (id) {
  return apiOrLocal(
    () => {
      try {
        return ApiBridge.request('GET', `/tickets/${id}`);
      } catch (error) {
        if (error.status === 404) return null;
        throw error;
      }
    },
    () => LocalDataStore.Tickets.getById(id)
  );
};

DataStore.Tickets.filter = function (options = {}) {
  return apiOrLocal(
    () => {
      const params = new URLSearchParams();
      if (options.status !== undefined) params.set('status', options.status);
      if (options.priority !== undefined) params.set('priority', options.priority);
      if (options.category !== undefined) params.set('category', options.category);
      if (options.userId !== undefined && options.userId !== null) params.set('userId', String(options.userId));
      if (options.assignedTo !== undefined && options.assignedTo !== null) params.set('assignedTo', String(options.assignedTo));
      if (options.search !== undefined) params.set('search', options.search);
      const query = params.toString();
      return ApiBridge.request('GET', `/tickets${query ? `?${query}` : ''}`) || [];
    },
    () => LocalDataStore.Tickets.filter(options)
  );
};

DataStore.Tickets.getOpenTickets = function () {
  return DataStore.Tickets.filter({ status: 'open' });
};

DataStore.Tickets.getInProgressTickets = function () {
  return DataStore.Tickets.filter({ status: 'in-progress' });
};

DataStore.Tickets.create = function (data) {
  return apiOrLocal(
    () => ApiBridge.request('POST', '/tickets', data),
    () => LocalDataStore.Tickets.create(data),
    false
  );
};

DataStore.Tickets.update = function (id, data) {
  return apiOrLocal(
    () => ApiBridge.request('PUT', `/tickets/${id}`, data),
    () => LocalDataStore.Tickets.update(id, data),
    false
  );
};

DataStore.Tickets.delete = function (id) {
  return apiOrLocal(
    () => {
      ApiBridge.request('DELETE', `/tickets/${id}`);
      return true;
    },
    () => LocalDataStore.Tickets.delete(id),
    false
  );
};

DataStore.Tickets.addComment = function (ticketId, userId, text) {
  return apiOrLocal(
    () => ApiBridge.request('POST', `/tickets/${ticketId}/comments`, { userId, text }),
    () => LocalDataStore.Tickets.addComment(ticketId, userId, text),
    false
  );
};

DataStore.Tickets.getStats = function () {
  return apiOrLocal(
    () => ApiBridge.request('GET', '/stats'),
    () => LocalDataStore.Tickets.getStats()
  );
};

DataStore.Categories.getAll = function () {
  return apiOrLocal(
    () => ApiBridge.request('GET', '/categories') || [],
    () => LocalDataStore.Categories.getAll()
  );
};

DataStore.Categories.add = function (name) {
  return apiOrLocal(
    () => ApiBridge.request('POST', '/categories', { name }),
    () => LocalDataStore.Categories.add(name),
    false
  );
};

/* ============================================================
   KİMLİK DOĞRULAMA (Authentication & Authorization)
   - Session/Cookie tabanlı login
   - Role-based: admin / support / user
   ============================================================ */

const Auth = (() => {
  const SESSION_KEY = 'ticketSystemSession';

  function login(email, password) {
    const user = DataStore.Users.authenticate(email, password);
    if (!user) return { success: false, message: 'E-posta veya şifre hatalı.' };
    const session = {
      userId: user.id,
      username: user.username,
      fullName: user.fullName,
      email: user.email,
      role: user.role,
      loginTime: new Date().toISOString()
    };
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
    return { success: true, user: session };
  }

  function logout() {
    sessionStorage.removeItem(SESSION_KEY);
  }

  function getSession() {
    const raw = sessionStorage.getItem(SESSION_KEY);
    return raw ? JSON.parse(raw) : null;
  }

  function isLoggedIn() {
    return getSession() !== null;
  }

  // Role-based yetkilendirme
  function hasRole(...roles) {
    const session = getSession();
    if (!session) return false;
    return roles.includes(session.role);
  }

  function isAdmin() { return hasRole('admin'); }
  function isSupport() { return hasRole('support', 'admin'); }
  function isUser() { return hasRole('user'); }

  // Sayfa koruma - yetkisiz erişim engelleme
  function requireAuth(redirectTo = 'login.html') {
    if (!isLoggedIn()) {
      window.location.href = redirectTo;
      return false;
    }
    return true;
  }

  function requireRole(roles, redirectTo = 'index.html') {
    if (!requireAuth()) return false;
    if (!hasRole(...roles)) {
      window.location.href = redirectTo;
      return false;
    }
    return true;
  }

  return { login, logout, getSession, isLoggedIn, hasRole, isAdmin, isSupport, isUser, requireAuth, requireRole };
})();

/* ============================================================
   VALIDASYON MOTORU (Data Annotations benzeri)
   Required, StringLength, Email, Pattern, MinLength
   ============================================================ */

const Validator = (() => {
  const rules = {
    required: (val, msg) => (!val || val.toString().trim() === '') ? (msg || 'Bu alan zorunludur.') : null,
    minLength: (val, min, msg) => (val && val.length < min) ? (msg || `En az ${min} karakter giriniz.`) : null,
    maxLength: (val, max, msg) => (val && val.length > max) ? (msg || `En fazla ${max} karakter giriniz.`) : null,
    email: (val, msg) => {
      const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      return (val && !re.test(val)) ? (msg || 'Geçerli bir e-posta adresi giriniz.') : null;
    },
    pattern: (val, regex, msg) => (val && !regex.test(val)) ? (msg || 'Geçersiz format.') : null,
    match: (val, other, msg) => (val !== other) ? (msg || 'Değerler eşleşmiyor.') : null
  };

  function validateField(value, fieldRules) {
    for (const rule of fieldRules) {
      let error = null;
      if (rule.type === 'required') error = rules.required(value, rule.message);
      else if (rule.type === 'minLength') error = rules.minLength(value, rule.value, rule.message);
      else if (rule.type === 'maxLength') error = rules.maxLength(value, rule.value, rule.message);
      else if (rule.type === 'email') error = rules.email(value, rule.message);
      else if (rule.type === 'pattern') error = rules.pattern(value, rule.value, rule.message);
      else if (rule.type === 'match') error = rules.match(value, rule.value, rule.message);
      if (error) return error;
    }
    return null;
  }

  function validateForm(formData, schema) {
    const errors = {};
    let valid = true;
    for (const [field, fieldRules] of Object.entries(schema)) {
      const error = validateField(formData[field], fieldRules);
      if (error) {
        errors[field] = error;
        valid = false;
      }
    }
    return { valid, errors };
  }

  // DOM üzerinde hata gösterme
  function showFieldError(inputEl, message) {
    inputEl.classList.add('is-invalid');
    inputEl.classList.remove('is-valid');
    let errEl = inputEl.parentElement.querySelector('.form-error');
    if (!errEl) {
      errEl = document.createElement('div');
      errEl.className = 'form-error';
      inputEl.parentElement.appendChild(errEl);
    }
    errEl.innerHTML = `<span>⚠</span> ${message}`;
  }

  function showFieldSuccess(inputEl) {
    inputEl.classList.remove('is-invalid');
    inputEl.classList.add('is-valid');
    const errEl = inputEl.parentElement.querySelector('.form-error');
    if (errEl) errEl.remove();
  }

  function clearFieldError(inputEl) {
    inputEl.classList.remove('is-invalid', 'is-valid');
    const errEl = inputEl.parentElement.querySelector('.form-error');
    if (errEl) errEl.remove();
  }

  function clearFormErrors(formEl) {
    formEl.querySelectorAll('.form-control').forEach(el => clearFieldError(el));
  }

  return { validateForm, validateField, showFieldError, showFieldSuccess, clearFieldError, clearFormErrors };
})();

/* ============================================================
   ENUM TİPLERİ
   ============================================================ */

const TicketStatus = {
  OPEN: 'open',
  IN_PROGRESS: 'in-progress',
  RESOLVED: 'resolved',
  CLOSED: 'closed',
  labels: {
    'open': 'Açık',
    'in-progress': 'İşlemde',
    'resolved': 'Çözüldü',
    'closed': 'Kapandı'
  },
  badgeClass: {
    'open': 'badge-open',
    'in-progress': 'badge-in-progress',
    'resolved': 'badge-resolved',
    'closed': 'badge-closed'
  }
};

const TicketPriority = {
  LOW: 'low',
  MEDIUM: 'medium',
  HIGH: 'high',
  CRITICAL: 'critical',
  labels: {
    'low': 'Düşük',
    'medium': 'Orta',
    'high': 'Yüksek',
    'critical': 'Kritik'
  },
  badgeClass: {
    'low': 'badge-low',
    'medium': 'badge-medium',
    'high': 'badge-high',
    'critical': 'badge-critical'
  },
  icons: {
    'low': '',
    'medium': '',
    'high': '',
    'critical': ''
  }
};

const UserRole = {
  ADMIN: 'admin',
  SUPPORT: 'support',
  USER: 'user',
  labels: {
    'admin': 'Yönetici',
    'support': 'Destek Ekibi',
    'user': 'Kullanıcı'
  }
};

/* ============================================================
   YARDIMCI FONKSİYONLAR
   ============================================================ */

const Utils = {
  formatDate(dateStr) {
    if (!dateStr) return '-';
    const d = new Date(dateStr);
    return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' });
  },
  formatDateTime(dateStr) {
    if (!dateStr) return '-';
    const d = new Date(dateStr);
    return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  },
  timeAgo(dateStr) {
    const now = new Date();
    const then = new Date(dateStr);
    const diff = Math.floor((now - then) / 1000);
    if (diff < 60) return 'Az önce';
    if (diff < 3600) return `${Math.floor(diff / 60)} dakika önce`;
    if (diff < 86400) return `${Math.floor(diff / 3600)} saat önce`;
    if (diff < 604800) return `${Math.floor(diff / 86400)} gün önce`;
    return Utils.formatDate(dateStr);
  },
  getInitials(name) {
    if (!name) return '?';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  },
  escapeHtml(text) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(text));
    return div.innerHTML;
  },
  generateId() {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  }
};

const ThemeManager = (() => {
  const KEY = 'ticketSystemTheme';

  function getTheme() {
    const stored = localStorage.getItem(KEY);
    return stored === 'dark' || stored === 'light' ? stored : 'light';
  }

  function setTheme(theme) {
    const next = theme === 'dark' ? 'dark' : 'light';
    document.documentElement.setAttribute('data-theme', next);
    document.documentElement.style.colorScheme = next;
    localStorage.setItem(KEY, next);
    updateButton();
  }

  function toggleTheme() {
    setTheme(getTheme() === 'dark' ? 'light' : 'dark');
  }

  function getLabel() {
    return getTheme() === 'dark' ? 'Açık Mod' : 'Koyu Mod';
  }

  function updateButton() {
    const btn = document.getElementById('themeToggleBtn');
    if (btn) btn.textContent = getLabel();
  }

  function init() {
    setTheme(getTheme());
  }

  return { init, toggleTheme, setTheme, getTheme, getLabel, updateButton };
})();

ThemeManager.init();

const EmojiCleaner = (() => {
  const emojiPattern = /[\u{1F000}-\u{1FAFF}\u{2600}-\u{27BF}\u{FE0F}\u{200D}]/gu;

  function cleanText(text) {
    return String(text || '')
      .replace(emojiPattern, '')
      .replace(/\s{2,}/g, ' ')
      .trim();
  }

  function cleanNode(node) {
    if (!node) return;
    if (node.nodeType === Node.TEXT_NODE) {
      const cleaned = cleanText(node.nodeValue);
      if (cleaned !== node.nodeValue) node.nodeValue = cleaned;
      return;
    }
    if (node.nodeType !== Node.ELEMENT_NODE) return;
    if (['SCRIPT', 'STYLE', 'TEXTAREA', 'INPUT'].includes(node.tagName)) return;
    node.childNodes.forEach(cleanNode);
  }

  function watch(root = document.body) {
    if (!root) return;
    cleanNode(root);
    const observer = new MutationObserver(mutations => {
      mutations.forEach(mutation => mutation.addedNodes.forEach(cleanNode));
    });
    observer.observe(root, { childList: true, subtree: true });
  }

  return { watch };
})();

/* ============================================================
   TOAST BİLDİRİM SİSTEMİ
   ============================================================ */

const Toast = (() => {
  let container;

  function getContainer() {
    if (!container) {
      container = document.createElement('div');
      container.className = 'toast-container';
      document.body.appendChild(container);
    }
    return container;
  }

  function show(message, type = 'info', duration = 3500) {
    const icons = { success: 'OK', danger: 'ERR', warning: 'UYARI', info: 'BILGI' };
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
      <span class="toast-icon">${icons[type] || icons.info}</span>
      <span class="toast-message">${message}</span>
      <span class="toast-close" onclick="this.parentElement.remove()">✕</span>
    `;
    getContainer().appendChild(toast);
    setTimeout(() => {
      toast.classList.add('removing');
      setTimeout(() => toast.remove(), 300);
    }, duration);
  }

  return {
    success: (msg, dur) => show(msg, 'success', dur),
    error: (msg, dur) => show(msg, 'danger', dur),
    warning: (msg, dur) => show(msg, 'warning', dur),
    info: (msg, dur) => show(msg, 'info', dur)
  };
})();

/* ============================================================
   MODAL YÖNETİCİSİ
   ============================================================ */

const Modal = {
  open(id) {
    const overlay = document.getElementById(id);
    if (overlay) {
      overlay.classList.add('active');
      document.body.style.overflow = 'hidden';
    }
  },
  close(id) {
    const overlay = document.getElementById(id);
    if (overlay) {
      overlay.classList.remove('active');
      document.body.style.overflow = '';
    }
  },
  closeAll() {
    document.querySelectorAll('.modal-overlay.active').forEach(m => {
      m.classList.remove('active');
    });
    document.body.style.overflow = '';
  }
};

// Modal dışına tıklayınca kapat
document.addEventListener('click', e => {
  if (e.target.classList.contains('modal-overlay')) Modal.closeAll();
});

/* ============================================================
   NAVBAR ORTAK BILEŞEN
   ============================================================ */

const NavbarComponent = {
  render() {
    const session = Auth.getSession();
    const navEl = document.getElementById('navbar');
    if (!navEl) return;

    const isAdminOrSupport = session && (session.role === 'admin' || session.role === 'support');
    const isAdmin = session && session.role === 'admin';

    const adminLinks = isAdminOrSupport ? `
      <a href="admin-dashboard.html" class="nav-link" data-page="admin-dashboard">
        Panel
      </a>
      <a href="admin-tickets.html" class="nav-link" data-page="admin-tickets">
        Tüm Talepler
      </a>
      ${isAdmin ? `<a href="admin-users.html" class="nav-link" data-page="admin-users">Kullanıcılar</a>` : ''}
    ` : '';

    const userLinks = session && session.role === 'user' ? `
      <a href="user-dashboard.html" class="nav-link" data-page="user-dashboard">
        Ana Sayfa
      </a>
      <a href="user-tickets.html" class="nav-link" data-page="user-tickets">
        Taleplerim
      </a>
      <a href="new-ticket.html" class="nav-link" data-page="new-ticket">
        Yeni Talep
      </a>
    ` : '';

    const roleBadgeClass = session ? `role-badge ${session.role}` : '';
    const roleLabel = session ? UserRole.labels[session.role] : '';

    navEl.innerHTML = `
      <div class="navbar-inner">
        <a href="${session ? (isAdminOrSupport ? 'admin-dashboard.html' : 'user-dashboard.html') : 'login.html'}" class="navbar-brand">
          <span>Destek<b>Hub</b></span>
        </a>
        <button class="hamburger" id="hamburgerBtn" aria-label="Menü">
          <span></span><span></span><span></span>
        </button>
        <nav class="navbar-nav" id="mainNav">
          ${adminLinks}
          ${userLinks}
        </nav>
        <div class="navbar-actions">
          <button type="button" class="btn btn-secondary btn-sm theme-toggle" id="themeToggleBtn" onclick="ThemeManager.toggleTheme()">
            ${ThemeManager.getTheme() === 'dark' ? 'Açık Mod' : 'Koyu Mod'}
          </button>
          ${session ? `
          <div class="navbar-user">
            <div class="dropdown" id="userDropdown">
              <div style="display:flex;align-items:center;gap:10px;cursor:pointer;" onclick="document.getElementById('userDropdown').classList.toggle('open')">
                <div class="user-info">
                  <span class="user-name">${Utils.escapeHtml(session.fullName)}</span>
                  <span class="${roleBadgeClass}">${roleLabel}</span>
                </div>
              </div>
              <div class="dropdown-menu">
                <a href="profile.html" class="dropdown-item">Profilim</a>
                <div class="dropdown-divider"></div>
                <button class="dropdown-item danger" onclick="NavbarComponent.handleLogout()">Çıkış Yap</button>
              </div>
            </div>
          </div>
        ` : `
          <div class="navbar-user">
            <a href="login.html" class="btn btn-primary btn-sm">Giriş Yap</a>
          </div>
        `}
        </div>
      </div>
    `;

    // Aktif linki işaretle
    const currentPage = window.location.pathname.split('/').pop().replace('.html', '');
    navEl.querySelectorAll('[data-page]').forEach(link => {
      if (link.dataset.page === currentPage) link.classList.add('active');
    });

    // Hamburger menü
    const hamburger = document.getElementById('hamburgerBtn');
    const mainNav = document.getElementById('mainNav');
    if (hamburger && mainNav) {
      hamburger.addEventListener('click', () => mainNav.classList.toggle('mobile-open'));
    }

    // Dropdown dışına tıklayınca kapat
    document.addEventListener('click', e => {
      const dd = document.getElementById('userDropdown');
      if (dd && !dd.contains(e.target)) dd.classList.remove('open');
    });
  },

  handleLogout() {
    Auth.logout();
    Toast.success('Başarıyla çıkış yapıldı.');
    setTimeout(() => window.location.href = 'login.html', 800);
  }
};

/* ============================================================
   FOOTER ORTAK BILEŞEN
   ============================================================ */

const FooterComponent = {
  render() {
    const footerEl = document.getElementById('footer');
    if (!footerEl) return;
    footerEl.innerHTML = `
      <div class="footer-inner">
        <div class="footer-brand">
          <span>DestekHub</span>
        </div>
        <div class="footer-links">
          <a href="#">Gizlilik</a>
          <a href="#">Kullanım Şartları</a>
          <a href="#">Yardım</a>
        </div>
        <div class="footer-copy">© 2024 DestekHub. Tüm hakları saklıdır.</div>
      </div>
    `;
  }
};

/* ============================================================
   UYGULAMA BAŞLATMA
   ============================================================ */

document.addEventListener('DOMContentLoaded', () => {
  DataStore.initDB();
  NavbarComponent.render();
  FooterComponent.render();
  EmojiCleaner.watch(document.body);
});

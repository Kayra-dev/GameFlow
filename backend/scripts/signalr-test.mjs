/**
 * GameFlow SignalR uçtan uca testi.
 *
 * İki canlı istemci bağlar ve gerçek zamanlı akışı doğrular: bir istemcinin
 * gönderdiği mesajın diğerine ulaşması, düzenleme/silme yayınları, "yazıyor"
 * göstergesi, çevrimiçi durum ve görev atama bildirimi.
 *
 * REST duman testi (smoke-test.sh) iş kurallarını doğrular; bu betik ise
 * yalnızca SignalR taşıma katmanını hedefler.
 *
 * Kullanım (backend dizininden):
 *   node scripts/signalr-test.mjs
 *   API=http://localhost:5080 node scripts/signalr-test.mjs
 */
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const API = process.env.API ?? 'http://localhost:5080';
const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? 'admin@gameflow.dev';
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? 'Admin!2345';

// @microsoft/signalr frontend bağımlılıkları arasında; backend'e ayrıca kurulmaz.
const here = path.dirname(fileURLToPath(import.meta.url));
const frontendRequire = createRequire(path.join(here, '../../frontend/package.json'));

let signalR;
try {
  signalR = frontendRequire('@microsoft/signalr');
} catch {
  console.error(
    'Hata: @microsoft/signalr bulunamadı.\n' +
      '      Önce frontend bağımlılıklarını kurun: cd ../frontend && npm install',
  );
  process.exit(1);
}

let passed = 0;
let failed = 0;

const color = process.stdout.isTTY
  ? { green: '\x1b[32m', red: '\x1b[31m', dim: '\x1b[2m', bold: '\x1b[1m', reset: '\x1b[0m' }
  : { green: '', red: '', dim: '', bold: '', reset: '' };

function section(title) {
  console.log(`\n${color.bold}${title}${color.reset}`);
}

function assert(description, condition, detail = '') {
  if (condition) {
    passed += 1;
    console.log(`  ${color.green}✓${color.reset} ${description}`);
  } else {
    failed += 1;
    console.log(
      `  ${color.red}✗${color.reset} ${description}` +
        (detail ? ` ${color.dim}(${detail})${color.reset}` : ''),
    );
  }
}

async function api(method, path, { token, body, expectStatus } = {}) {
  const headers = {};
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body) headers['Content-Type'] = 'application/json';

  const response = await fetch(`${API}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (expectStatus && response.status !== expectStatus) {
    const text = await response.text();
    throw new Error(`${method} ${path} → ${response.status} (beklenen ${expectStatus}): ${text}`);
  }

  if (response.status === 204) return null;

  const text = await response.text();
  return text ? JSON.parse(text) : null;
}

/** Belirli bir hub olayının gelmesini bekler; süre aşılırsa null döner. */
function waitForEvent(connection, eventName, timeoutMs = 5000) {
  return new Promise((resolve) => {
    const timer = setTimeout(() => {
      connection.off(eventName, handler);
      resolve(null);
    }, timeoutMs);

    function handler(...args) {
      clearTimeout(timer);
      connection.off(eventName, handler);
      resolve(args);
    }

    connection.on(eventName, handler);
  });
}

function buildConnection(hubPath, token) {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${API}${hubPath}?access_token=${encodeURIComponent(token)}`, {
      // Node ortamında WebSocket kullanılır; negotiate adımı korunur.
      skipNegotiation: false,
      transport: signalR.HttpTransportType.WebSockets,
    })
    .configureLogging(signalR.LogLevel.None)
    .build();
}

const created = { userIds: [], teamId: null, projectId: null };
const connections = [];

async function cleanup(adminToken) {
  for (const connection of connections) {
    try {
      await connection.stop();
    } catch {
      // Bağlantı zaten kapalıysa yoksayılır.
    }
  }

  if (!adminToken) return;

  try {
    if (created.projectId) {
      await api('DELETE', `/api/projects/${created.projectId}`, { token: adminToken });
    }
    if (created.teamId) {
      await api('DELETE', `/api/teams/${created.teamId}`, { token: adminToken });
    }
    for (const userId of created.userIds) {
      await api('DELETE', `/api/users/${userId}`, { token: adminToken });
    }
  } catch (error) {
    console.log(`${color.dim}Temizlik sırasında hata: ${error.message}${color.reset}`);
  }
}

async function main() {
  console.log(`${color.bold}GameFlow SignalR testi${color.reset}  ${color.dim}${API}${color.reset}`);

  // ------------------------------------------------------------- Hazırlık
  section('1. Hazırlık');

  const health = await fetch(`${API}/health`).catch(() => null);
  if (!health?.ok) {
    console.error(
      `  ${color.red}✗${color.reset} API ayakta değil. Önce şunu çalıştırın:\n` +
        `      cd src/GameFlow.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls ${API}`,
    );
    process.exit(1);
  }
  assert('API ayakta', true);

  const adminAuth = await api('POST', '/api/auth/login', {
    body: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
    expectStatus: 200,
  });
  const adminToken = adminAuth.accessToken;
  const adminId = adminAuth.user.id;
  assert('Yönetici girişi', Boolean(adminToken));

  const suffix = Date.now().toString().slice(-6);

  // Takım lideri olarak ikinci bir kullanıcı: sohbet odasına iki taraf gerekir.
  const member = await api('POST', '/api/users', {
    token: adminToken,
    body: {
      fullName: `SignalR Test ${suffix}`,
      email: `signalr-${suffix}@gameflow.dev`,
      password: 'SignalRTest1',
      role: 3,
      mustChangePassword: false,
    },
    expectStatus: 201,
  });
  created.userIds.push(member.id);

  const team = await api('POST', '/api/teams', {
    token: adminToken,
    body: { name: `SignalR Takımı ${suffix}`, category: 1, colorHex: '#6366F1' },
    expectStatus: 201,
  });
  created.teamId = team.id;

  // Her iki kullanıcı da odaya erişebilmek için takıma eklenir.
  await api('POST', `/api/teams/${team.id}/members`, {
    token: adminToken,
    body: { userIds: [adminId, member.id] },
    expectStatus: 200,
  });

  const project = await api('POST', '/api/projects', {
    token: adminToken,
    body: { name: `SignalR Projesi ${suffix}`, key: `SR${suffix.slice(-4)}`, colorHex: '#8B5CF6' },
    expectStatus: 201,
  });
  created.projectId = project.id;

  await api('POST', `/api/projects/${project.id}/members`, {
    token: adminToken,
    body: { userIds: [member.id], isManager: false },
    expectStatus: 200,
  });

  const memberAuth = await api('POST', '/api/auth/login', {
    body: { email: `signalr-${suffix}@gameflow.dev`, password: 'SignalRTest1' },
    expectStatus: 200,
  });
  const memberToken = memberAuth.accessToken;

  const rooms = await api('GET', '/api/chat/rooms', { token: adminToken, expectStatus: 200 });
  const roomId = rooms.find((room) => room.teamId === team.id)?.id;
  assert('Takım sohbet odası hazır', Boolean(roomId));

  // ------------------------------------------------------------ Bağlantı
  section('2. Hub bağlantıları');

  const adminChat = buildConnection('/hubs/chat', adminToken);
  const memberChat = buildConnection('/hubs/chat', memberToken);
  const adminPresence = buildConnection('/hubs/presence', adminToken);
  const memberPresence = buildConnection('/hubs/presence', memberToken);
  connections.push(adminChat, memberChat, adminPresence, memberPresence);

  await adminChat.start();
  await memberChat.start();
  assert('İki istemci sohbet hub’ına bağlandı', adminChat.state === 'Connected' && memberChat.state === 'Connected');

  // Yetkisiz bağlantı reddedilmeli.
  const unauthorized = buildConnection('/hubs/chat', 'gecersiz-token');
  let unauthorizedFailed = false;
  try {
    await unauthorized.start();
  } catch {
    unauthorizedFailed = true;
  }
  assert('Geçersiz token ile bağlantı reddedilir', unauthorizedFailed);

  await adminChat.invoke('JoinRoom', roomId);
  await memberChat.invoke('JoinRoom', roomId);
  assert('Her iki istemci odaya katıldı', true);

  // Erişimi olmayan odaya katılım reddedilmeli.
  const leadersRoom = rooms.find((room) => room.type === 2);
  if (leadersRoom) {
    let joinRejected = false;
    try {
      await memberChat.invoke('JoinRoom', leadersRoom.id);
    } catch {
      joinRejected = true;
    }
    assert('Yetkisi olmayan oda için JoinRoom reddedilir', joinRejected);
  }

  // ------------------------------------------------------- Anlık mesajlaşma
  section('3. Gerçek zamanlı mesajlaşma');

  const receivedPromise = waitForEvent(memberChat, 'MessageReceived');
  const sent = await adminChat.invoke('SendMessage', roomId, {
    content: 'SignalR üzerinden gönderilen mesaj',
  });
  const received = await receivedPromise;

  assert('Karşı taraf mesajı anında aldı', received !== null);
  assert(
    'Alınan mesaj gönderilenle aynı',
    received?.[0]?.id === sent.id,
    `gönderilen ${sent.id}, alınan ${received?.[0]?.id}`,
  );
  assert('Mesaj gönderen bilgisi taşıyor', received?.[0]?.sender?.id === adminId);

  const editedPromise = waitForEvent(memberChat, 'MessageEdited');
  await adminChat.invoke('EditMessage', roomId, sent.id, { content: 'Düzenlenmiş mesaj' });
  const edited = await editedPromise;

  assert('Düzenleme yayınlandı', edited !== null);
  assert('Düzenlenen içerik güncel', edited?.[0]?.content === 'Düzenlenmiş mesaj');
  assert('isEdited bayrağı işaretli', edited?.[0]?.isEdited === true);

  const typingPromise = waitForEvent(memberChat, 'UserTyping');
  await adminChat.invoke('NotifyTyping', roomId, true);
  const typing = await typingPromise;

  assert('"Yazıyor" göstergesi iletildi', typing !== null);
  assert('Yazan kullanıcı doğru', typing?.[1] === adminId);

  // Okundu bilgisi karşı tarafa yayınlanmalı.
  const readPromise = waitForEvent(adminChat, 'MessagesRead');
  const remainingUnread = await memberChat.invoke('MarkAsRead', roomId, { messageIds: [] });
  const readEvent = await readPromise;

  assert('Okundu bilgisi yayınlandı', readEvent !== null);
  assert('Okunmamış sayısı sıfırlandı', remainingUnread === 0, `gelen ${remainingUnread}`);

  const deletedPromise = waitForEvent(memberChat, 'MessageDeleted');
  await adminChat.invoke('DeleteMessage', roomId, sent.id);
  const deleted = await deletedPromise;

  assert('Silme yayınlandı', deleted !== null);
  assert('Silinen mesaj kimliği doğru', deleted?.[1] === sent.id);

  // ---------------------------------------------------------- Çevrimiçi durum
  section('4. Çevrimiçi durum');

  await adminPresence.start();
  const onlinePromise = waitForEvent(adminPresence, 'UserOnline');
  await memberPresence.start();
  const online = await onlinePromise;

  assert('Bağlanan kullanıcı çevrimiçi bildirildi', online !== null);
  assert('Çevrimiçi olan kullanıcı doğru', online?.[0] === member.id);

  const onlineUsers = await adminPresence.invoke('GetOnlineUsers');
  assert('Çevrimiçi liste iki kullanıcı içeriyor', onlineUsers.length >= 2, `gelen ${onlineUsers.length}`);

  const dashboard = await api('GET', '/api/dashboard?onlyMyTasks=false', {
    token: adminToken,
    expectStatus: 200,
  });
  assert(
    'Dashboard çevrimiçi kullanıcıyı gösteriyor',
    dashboard.onlineUsers.some((user) => user.id === member.id),
  );

  // ------------------------------------------------------------- Bildirim
  section('5. Gerçek zamanlı bildirim');

  const notificationPromise = waitForEvent(memberPresence, 'NotificationReceived');
  const unreadPromise = waitForEvent(memberPresence, 'UnreadCountChanged');

  await api('POST', '/api/work-items', {
    token: adminToken,
    body: {
      projectId: project.id,
      title: 'SignalR bildirim testi görevi',
      assigneeId: member.id,
      teamId: team.id,
    },
    expectStatus: 201,
  });

  const notification = await notificationPromise;
  const unread = await unreadPromise;

  assert('Görev ataması bildirimi anında ulaştı', notification !== null);
  assert('Bildirim türü "görev atandı" (1)', notification?.[0]?.type === 1);
  assert('Bildirim bağlantısı üretildi', Boolean(notification?.[0]?.link));
  assert('Okunmamış sayısı iletildi', unread !== null && unread[0] >= 1);

  const offlinePromise = waitForEvent(adminPresence, 'UserOffline');
  await memberPresence.stop();
  const offline = await offlinePromise;

  assert('Ayrılan kullanıcı çevrimdışı bildirildi', offline !== null);
  assert('Çevrimdışı olan kullanıcı doğru', offline?.[0] === member.id);

  // -------------------------------------------------------------- Temizlik
  await cleanup(adminToken);

  console.log(
    `\n${color.green}${passed} test geçti${color.reset}` +
      (failed > 0 ? `, ${color.red}${failed} test başarısız${color.reset}\n` : ', başarısız yok.\n'),
  );

  process.exit(failed > 0 ? 1 : 0);
}

main().catch(async (error) => {
  console.error(`\n${color.red}Test çalıştırılamadı:${color.reset} ${error.message}`);
  await cleanup(null);
  process.exit(1);
});

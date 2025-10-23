import http from 'k6/http';
import { check, sleep } from 'k6';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';
import { uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';
import { htmlReport } from 'https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.1/index.js';

export const options = {
  stages: [
    { duration: '10s', target: 5 },    // Плавный рост
    { duration: '30s', target: 10 },    // Средняя нагрузка
    { duration: '10s', target: 15 },   // Пиковая нагрузка
    { duration: '10s', target: 0 },    // Завершение
  ],
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<50'],
  },
};

// Используем имя сервиса из docker-compose
const BASE_URL = 'http://webcli:5234/api/v1';

const commonParams = {
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: '30s',
};

export default function () {
  const userData = generateUserData();
  executeUserScenario(userData);
  sleep(1);
}

function generateUserData() {
  const uuid = uuidv4().replace(/-/g, '').substring(0, 16);
  const username = `user_${uuid}`;
  const password = `pass_${uuid}`;
  const phoneNumber = `+7${randomIntBetween(9000000000, 9999999999)}`;
  
  return { username, password, phoneNumber };
}

function executeUserScenario(userData) {
  // ШАГ 1: Регистрация
  const registrationSuccess = registerUser(userData);
  if (!registrationSuccess) return;
  sleep(0.5);

  // ШАГ 2: Вход
  const loginSuccess = loginUser(userData);
  if (!loginSuccess) return;
  sleep(0.5);

  // ШАГ 3: Добавление привычки
  addHabit(userData);
  sleep(0.5);

  // ШАГ 4: Удаление всех привычек
  deleteAllHabits(userData);
  sleep(0.5);

  // ШАГ 5: Удаление аккаунта
  deleteAccount(userData);
}

function registerUser(userData) {
  const payload = JSON.stringify({
    userName: userData.username,
    phoneNumber: userData.phoneNumber,
    password: userData.password
  });
  
  const response = http.post(`${BASE_URL}/auth/register`, payload, commonParams);
  
  return check(response, {
    'registration status is 200': (r) => r.status === 200,
  });
}

function loginUser(userData) {
  const payload = JSON.stringify({
    userName: userData.username,
    password: userData.password
  });
  
  const response = http.post(`${BASE_URL}/auth/login`, payload, commonParams);
  
  return check(response, {
    'login status is 200': (r) => r.status === 200,
  });
}

function addHabit(userData) {
  const habitName = `habit_${randomIntBetween(1000, 9999)}`;
  
  const payload = JSON.stringify({
    userNameID: userData.username,
    name: habitName,
    minsToComplete: randomIntBetween(15, 120),
    prefFixedTimings: generateTimings(),
	Option: 0,
	countInWeek: 3
  });
  
  const response = http.post(
    `${BASE_URL}/users/${userData.username}/habits`,
    payload,
    commonParams
  );
  
  check(response, {
    'add habit status is 200': (r) => r.status === 200,
  });
}

function deleteAllHabits(userData) {
  const response = http.del(
    `${BASE_URL}/users/${userData.username}/habits`,
    null,
    commonParams
  );
  
  check(response, {
    'delete all habits status is 200': (r) => r.status === 200,
  });
}

function deleteAccount(userData) {
  const response = http.del(
    `${BASE_URL}/users/${userData.username}`,
    null,
    commonParams
  );
  
  check(response, {
    'delete account status is 204': (r) => r.status === 204,
  });
}

function generateTimings() {
  const timeSlots = [
    { Start: "09:00:00", End: "10:00:00" },
    { Start: "14:00:00", End: "15:00:00" },
    { Start: "18:00:00", End: "19:00:00" },
    { Start: "20:00:00", End: "21:00:00" }
  ];
  return timeSlots;
}

export function handleSummary(data) {
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  
  console.log('Saving results to /scripts/results/');
  
  return {
    [`/scripts/results/${timestamp}_summary.json`]: JSON.stringify(data, null, 2),
    
    //[`/scripts/results/${timestamp}_report.html`]: htmlReport(data),
    
    //[`/scripts/results/${timestamp}_summary.txt`]: textSummary(data, { indent: ' ', enableColors: false }),
    
    //'stdout': textSummary(data, { indent: ' ', enableColors: true }),
  };
}
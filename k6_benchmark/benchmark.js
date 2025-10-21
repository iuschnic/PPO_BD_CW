import http from 'k6/http';
import { check, sleep } from 'k6';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

export const options = {
  stages: [
    { duration: '30s', target: 5 },    // Плавный рост
    { duration: '1m', target: 10 },    // Средняя нагрузка
    { duration: '30s', target: 15 },   // Пиковая нагрузка
    { duration: '20s', target: 0 },    // Завершение
  ],
  thresholds: {
    http_req_failed: ['rate<0.1'],     // Допускаем 10% ошибок
    http_req_duration: ['p(95)<3000'], // 95% запросов < 3 сек
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
  const userId = randomIntBetween(100000, 999999);
  const username = `user_${userId}`;
  const password = `pass_${userId}`;
  const phoneNumber = `+7${randomIntBetween(9000000000, 9999999999)}`;
  
  return { username, password, phoneNumber, userId };
}

function executeUserScenario(userData) {
  // ШАГ 1: Регистрация
  const registrationSuccess = registerUser(userData);
  if (!registrationSuccess) return;

  // ШАГ 2: Вход
  const loginSuccess = loginUser(userData);
  if (!loginSuccess) return;

  // ШАГ 3: Добавление привычки
  addHabit(userData);

  // ШАГ 4: Удаление всех привычек
  deleteAllHabits(userData);

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
    duration: randomIntBetween(15, 120),
    prefFixedTimings: generateTimings()
  });
  
  const response = http.post(
    `${BASE_URL}/users/${userData.username}/habits`,
    payload,
    commonParams
  );
  console.log(`name: ${userData.username}, ${payload.userNameID}, ${payload.name}, ${payload.duration}`);
  console.log(`Status: ${response.status}, Body: ${response.body}`);
  
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
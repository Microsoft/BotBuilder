const runTracker = require('./src');

const path = require('path');
const ENV_FILE = path.join(__dirname, '.env');
require('dotenv').config({ path: ENV_FILE });
process.env.BotBaseUrl = 'http://localhost:3978';

runTracker(console);

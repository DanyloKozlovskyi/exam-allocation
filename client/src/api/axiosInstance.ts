import axios from 'axios';
import { appConfig } from '../constants/config';

export const api = axios.create({
  baseURL: appConfig.apiUrl,
});

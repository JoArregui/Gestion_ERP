import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

const tokenKey = 'erp_auth_token';
const empresaKey = 'erp_empresa_id';

String apiBaseUrl() {
  const configured = String.fromEnvironment('ERP_API_BASE_URL');
  if (configured.isNotEmpty) return configured.endsWith('/') ? configured : '$configured/';
  return 'http://localhost:5109/';
}

class AuthService {
  AuthService({http.Client? client, FlutterSecureStorage? storage})
      : client = client ?? http.Client(),
        storage = storage ?? const FlutterSecureStorage();
  final http.Client client;
  final FlutterSecureStorage storage;

  Future<String?> restoreToken() => storage.read(key: tokenKey);

  Future<String> login(String email, String password) async {
    final response = await client.post(
      Uri.parse('${apiBaseUrl()}api/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
    );
    final body = jsonDecode(response.body) as Map<String, dynamic>?;
    final token = body?['token'] as String?;
    if (response.statusCode < 200 || response.statusCode >= 300 || token == null || token.isEmpty) {
      throw Exception(body?['errorMessage'] ?? body?['message'] ?? 'No se pudo iniciar sesión.');
    }
    await storage.write(key: tokenKey, value: token);
    return token;
  }

  Future<void> logout() async {
    await storage.delete(key: tokenKey);
    await storage.delete(key: empresaKey);
  }

  // Decodifica payload JWT para extraer claims sin verificar firma
  static Map<String, dynamic> parseJwt(String token) {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return {};
      var payload = parts[1].replaceAll('-', '+').replaceAll('_', '/');
      payload = payload.padRight(payload.length + (4 - payload.length % 4) % 4, '=');
      final jsonStr = utf8.decode(base64.decode(payload));
      return jsonDecode(jsonStr) as Map<String, dynamic>;
    } catch (_) {
      return {};
    }
  }

  static bool needsOnboarding(String token) {
    final claims = parseJwt(token);
    final empresaId = claims['EmpresaId'] ?? claims['empresaId'] ?? claims['empresa_id'];
    return empresaId == null || empresaId.toString() == '0' || empresaId.toString().isEmpty;
  }
}

class ApiClient {
  ApiClient({required this.token, http.Client? client}) : client = client ?? http.Client();
  final String token;
  final http.Client client;

  Map<String, String> get headers => {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      };

  Future<Map<String, dynamic>?> getJson(String path) async {
    try {
      final res = await client.get(Uri.parse('${apiBaseUrl()}$path'), headers: headers);
      if (res.statusCode >= 200 && res.statusCode < 300) {
        return jsonDecode(res.body) as Map<String, dynamic>;
      }
    } catch (_) {}
    return null;
  }

  Future<List<dynamic>?> getList(String path) async {
    try {
      final res = await client.get(Uri.parse('${apiBaseUrl()}$path'), headers: headers);
      if (res.statusCode >= 200 && res.statusCode < 300) {
        final decoded = jsonDecode(res.body);
        if (decoded is List) return decoded;
        if (decoded is Map && decoded['data'] is List) return decoded['data'] as List;
      }
    } catch (_) {}
    return null;
  }
}

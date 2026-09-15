using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERP.Domain.Dtos
{
    // ============================================
    // DTOs DE AUTENTICACIÓN
    // ============================================
    
    public class LoginDto
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio para iniciar sesión.")]
        [EmailAddress(ErrorMessage = "Debe introducir un formato de correo electrónico válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio para el registro.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe asignar un rol al usuario.")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
            ErrorMessage = "La contraseña debe contener al menos una mayúscula, una minúscula, un número y un carácter especial.")]
        public string Password { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El ID de empresa es obligatorio para vincular al usuario.")]
        public int EmpresaId { get; set; }
    }

    // ============================================
    // DTOs DE GESTIÓN DE USUARIOS
    // ============================================
    
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public int? EmpresaId { get; set; }
        public List<int> EmpresaIds { get; set; } = new();
        public List<string> EmpresaNombres { get; set; } = new();
    }

    public class CreateUserDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        public string Email { get; set; } = string.Empty;

        public string? Password { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio para definir permisos.")]
        public string Role { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Debe especificar al menos una empresa.")]
        public int EmpresaId { get; set; }
        // Multi-empresa: si se envía, se asignan todas; si solo viene EmpresaId, se usa esa
        public List<int>? EmpresaIds { get; set; }
    }

    // ============================================
    // DTOs DE ROLES Y PERMISOS (RBAC)
    // ============================================
    
    public class RoleDto
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;
        
        public int UserCount { get; set; }
    }

    /// <summary>
    /// Transporta la matriz completa de permisos para un rol específico.
    /// </summary>
    public class RolePermissionDto
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<PermissionItemDto> Permissions { get; set; } = new();
    }

    /// <summary>
    /// Representa un permiso individual dentro de la matriz.
    /// </summary>
    public class PermissionItemDto
    {
        public string Value { get; set; } = string.Empty;       // Ej: "Ventas.Crear"
        public string Description { get; set; } = string.Empty; // Ej: "Crear nuevas facturas"
        public string Group { get; set; } = string.Empty;       // Ej: "Módulo de Facturación"
        public bool IsSelected { get; set; }                    // Estado del switch en la UI
    }

    // ============================================
    // RESPUESTA DE AUTENTICACIÓN
    // ============================================

    public class AuthResponseDto
    {
        public bool IsAuthSuccessful { get; set; }
        public string? Token { get; set; }
        public string? ErrorMessage { get; set; }
        
        // Opcional: Puedes incluir datos básicos para no tener que decodificar el JWT siempre
        public string? Email { get; set; }
        public string? FullName { get; set; }
    }
}
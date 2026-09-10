using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    public class UserEmpresa
    {
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }
    }
}

using System;

namespace ERP.Web.Services
{
    public enum NotificationType 
    { 
        Success, 
        Error, 
        Warning, 
        Info 
    }

    public class NotificationService
    {
        private readonly List<NotificationEntry> history = new();

        // Evento al que se suscribirá el MainLayout o componentes locales
        public event Action<string, NotificationType>? OnShow;

        public IReadOnlyList<NotificationEntry> History => history;

        /// <summary>
        /// Lanza una notificación visual en la interfaz de usuario.
        /// </summary>
        /// <param name="message">Mensaje a mostrar</param>
        /// <param name="type">Tipo de alerta (Success, Error, etc.)</param>
        public void ShowNotification(string message, NotificationType type)
        {
            history.Insert(0, new NotificationEntry(message, type, DateTime.Now));
            if (history.Count > 30)
                history.RemoveAt(history.Count - 1);

            OnShow?.Invoke(message, type);
        }

        // Métodos de conveniencia para mantener el código limpio
        public void Success(string message) => ShowNotification(message, NotificationType.Success);
        public void Error(string message) => ShowNotification(message, NotificationType.Error);
        public void Warning(string message) => ShowNotification(message, NotificationType.Warning);
        public void Info(string message) => ShowNotification(message, NotificationType.Info);

        public void ClearHistory() => history.Clear();
    }

    public sealed record NotificationEntry(string Message, NotificationType Type, DateTime CreatedAt);
}
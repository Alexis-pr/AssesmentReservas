namespace AssesmentReservas.API.Enums;

/// <summary>Eventos clave que disparan notificaciones omnicanal.</summary>
public enum NotificationType
{
    BookingConfirmed = 0,
    BookingCancelled = 1,
    KycApproved = 2,
    KycRejected = 3,
    CheckInReminder = 4,
    CheckOutReminder = 5
}

namespace NovaTech.TerraTech.Platform.CommercialManagement.Application.Errors;
public enum CommercialError
{
    NotFound,
    DuplicateOrder,
    InvalidProductId,
    InvalidOrderStatus,
    InvalidPaymentMethod,
    InsufficientStock,
    StockConflict,
    OrderValidationFailed,
    SubscriptionNotFound,
    PaymentFailed,
    UnexpectedError
}

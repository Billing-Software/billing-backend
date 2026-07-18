using Microsoft.AspNetCore.Mvc;

namespace BillingBackend.Controllers
{
    /// <summary>
    /// General settings controller.
    /// WhatsApp settings have been moved to the dedicated WhatsAppController.
    /// This controller is reserved for future non-WhatsApp settings endpoints.
    /// </summary>
    public class SettingsController : BaseApiController
    {
        // WhatsApp endpoints have moved to WhatsAppController:
        //   POST /api/whatsapp/connect
        //   GET  /api/whatsapp/status
        //   DELETE /api/whatsapp/disconnect
        //   POST /api/whatsapp/send-text
        //   POST /api/whatsapp/send-document
        //   POST /api/whatsapp/send-template
        //   GET  /api/whatsapp/messages
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caupo.Fiscal;


// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Lpfr
{
    [JsonProperty("apiKey", NullValueHandling = NullValueHandling.Ignore)]
    public string apiKey { get; set; }

    [JsonProperty("authorizeLocalClients", NullValueHandling = NullValueHandling.Ignore)]
    public bool? authorizeLocalClients { get; set; }

    [JsonProperty("authorizeRemoteClients", NullValueHandling = NullValueHandling.Ignore)]
    public bool? authorizeRemoteClients { get; set; }

    [JsonProperty("availableSmartCardReaders", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> availableSmartCardReaders { get; set; }

    [JsonProperty("canHaveMultipleSmartCardReaders", NullValueHandling = NullValueHandling.Ignore)]
    public bool? canHaveMultipleSmartCardReaders { get; set; }

    [JsonProperty("externalStorageFolder", NullValueHandling = NullValueHandling.Ignore)]
    public object externalStorageFolder { get; set; }

    [JsonProperty("languages", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> languages { get; set; }

    [JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
    public string password { get; set; }

    [JsonProperty("smartCardReaderName", NullValueHandling = NullValueHandling.Ignore)]
    public object smartCardReaderName { get; set; }

    [JsonProperty("username", NullValueHandling = NullValueHandling.Ignore)]
    public string username { get; set; }
}

public class LpfrStatus
{
    [JsonProperty("allowedPaymentTypes", NullValueHandling = NullValueHandling.Ignore)]
    public List<int?> allowedPaymentTypes { get; set; }

    [JsonProperty("apiKey", NullValueHandling = NullValueHandling.Ignore)]
    public string apiKey { get; set; }

    [JsonProperty("Gsc", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Gsc { get; set; }

    [JsonProperty("applicationLanguage", NullValueHandling = NullValueHandling.Ignore)]
    public string applicationLanguage { get; set; }

    [JsonProperty("authorizeLocalClients", NullValueHandling = NullValueHandling.Ignore)]
    public bool? authorizeLocalClients { get; set; }

    [JsonProperty("authorizeRemoteClients", NullValueHandling = NullValueHandling.Ignore)]
    public bool? authorizeRemoteClients { get; set; }

    [JsonProperty("availableDisplayDevices", NullValueHandling = NullValueHandling.Ignore)]
    public List<object> availableDisplayDevices { get; set; }

    [JsonProperty("availableEftPosDevices", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> availableEftPosDevices { get; set; }

    [JsonProperty("availableEftPosProtocols", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> availableEftPosProtocols { get; set; }

    [JsonProperty("availablePrinterTypes", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> availablePrinterTypes { get; set; }

    [JsonProperty("availablePrinters", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> availablePrinters { get; set; }

    [JsonProperty("availableScaleDevices", NullValueHandling = NullValueHandling.Ignore)]
    public List<object> availableScaleDevices { get; set; }

    [JsonProperty("availableScaleProtocols", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> availableScaleProtocols { get; set; }

    [JsonProperty("customTabName", NullValueHandling = NullValueHandling.Ignore)]
    public object customTabName { get; set; }

    [JsonProperty("customTabUrl", NullValueHandling = NullValueHandling.Ignore)]
    public object customTabUrl { get; set; }

    [JsonProperty("displayDeviceName", NullValueHandling = NullValueHandling.Ignore)]
    public object displayDeviceName { get; set; }

    [JsonProperty("displayDeviceRs232BaudRate", NullValueHandling = NullValueHandling.Ignore)]
    public object displayDeviceRs232BaudRate { get; set; }

    [JsonProperty("displayDeviceRs232DataBits", NullValueHandling = NullValueHandling.Ignore)]
    public object displayDeviceRs232DataBits { get; set; }

    [JsonProperty("displayDeviceRs232HardwareFlowControl", NullValueHandling = NullValueHandling.Ignore)]
    public object displayDeviceRs232HardwareFlowControl { get; set; }

    [JsonProperty("displayDeviceRs232Parity", NullValueHandling = NullValueHandling.Ignore)]
    public object displayDeviceRs232Parity { get; set; }

    [JsonProperty("displayDeviceRs232StopBits", NullValueHandling = NullValueHandling.Ignore)]
    public object displayDeviceRs232StopBits { get; set; }

    [JsonProperty("displayEnabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? displayEnabled { get; set; }

    [JsonProperty("displayHandler", NullValueHandling = NullValueHandling.Ignore)]
    public object displayHandler { get; set; }

    [JsonProperty("displayProtocol", NullValueHandling = NullValueHandling.Ignore)]
    public object displayProtocol { get; set; }

    [JsonProperty("displayTextCodePage", NullValueHandling = NullValueHandling.Ignore)]
    public object displayTextCodePage { get; set; }

    [JsonProperty("displayTextCols", NullValueHandling = NullValueHandling.Ignore)]
    public object displayTextCols { get; set; }

    [JsonProperty("displayTextRows", NullValueHandling = NullValueHandling.Ignore)]
    public object displayTextRows { get; set; }

    [JsonProperty("eftPosCredentials", NullValueHandling = NullValueHandling.Ignore)]
    public object eftPosCredentials { get; set; }

    [JsonProperty("eftPosDeviceName", NullValueHandling = NullValueHandling.Ignore)]
    public object eftPosDeviceName { get; set; }

    [JsonProperty("eftPosDeviceRs232BaudRate", NullValueHandling = NullValueHandling.Ignore)]
    public object eftPosDeviceRs232BaudRate { get; set; }

    [JsonProperty("eftPosDeviceRs232DataBits", NullValueHandling = NullValueHandling.Ignore)]
    public object eftPosDeviceRs232DataBits { get; set; }

    [JsonProperty("eftPosDeviceRs232HardwareFlowControl", NullValueHandling = NullValueHandling.Ignore)]
    public object eftPosDeviceRs232HardwareFlowControl { get; set; }

    [JsonProperty("eftPosDeviceRs232Parity", NullValueHandling = NullValueHandling.Ignore)]
    public object eftPosDeviceRs232Parity { get; set; }

    [JsonProperty("eftPosDeviceRs232StopBits", NullValueHandling = NullValueHandling.Ignore)]
    public object eftPosDeviceRs232StopBits { get; set; }

    [JsonProperty("eftPosProtocol", NullValueHandling = NullValueHandling.Ignore)]
    public object eftPosProtocol { get; set; }

    [JsonProperty("issueCopyOnRefund", NullValueHandling = NullValueHandling.Ignore)]
    public bool? issueCopyOnRefund { get; set; }

    [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
    public string language { get; set; }

    [JsonProperty("languages", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> languages { get; set; }

    [JsonProperty("lpfr", NullValueHandling = NullValueHandling.Ignore)]
    public Lpfr lpfr { get; set; }

    [JsonProperty("lpfrUrl", NullValueHandling = NullValueHandling.Ignore)]
    public string lpfrUrl { get; set; }

    [JsonProperty("paperHeight", NullValueHandling = NullValueHandling.Ignore)]
    public object paperHeight { get; set; }

    [JsonProperty("paperMargin", NullValueHandling = NullValueHandling.Ignore)]
    public object paperMargin { get; set; }

    [JsonProperty("paperWidth", NullValueHandling = NullValueHandling.Ignore)]
    public object paperWidth { get; set; }

    [JsonProperty("posName", NullValueHandling = NullValueHandling.Ignore)]
    public object posName { get; set; }

    [JsonProperty("printerDpi", NullValueHandling = NullValueHandling.Ignore)]
    public object printerDpi { get; set; }

    [JsonProperty("printerName", NullValueHandling = NullValueHandling.Ignore)]
    public object printerName { get; set; }

    [JsonProperty("printerType", NullValueHandling = NullValueHandling.Ignore)]
    public string printerType { get; set; }

    [JsonProperty("qrCodeSize", NullValueHandling = NullValueHandling.Ignore)]
    public object qrCodeSize { get; set; }

    [JsonProperty("receiptCustomCommandBegin", NullValueHandling = NullValueHandling.Ignore)]
    public object receiptCustomCommandBegin { get; set; }

    [JsonProperty("receiptCustomCommandEnd", NullValueHandling = NullValueHandling.Ignore)]
    public object receiptCustomCommandEnd { get; set; }

    [JsonProperty("receiptCutPaper", NullValueHandling = NullValueHandling.Ignore)]
    public string receiptCutPaper { get; set; }

    [JsonProperty("receiptDiscountText", NullValueHandling = NullValueHandling.Ignore)]
    public object receiptDiscountText { get; set; }

    [JsonProperty("receiptFeedLinesBegin", NullValueHandling = NullValueHandling.Ignore)]
    public int? receiptFeedLinesBegin { get; set; }

    [JsonProperty("receiptFeedLinesEnd", NullValueHandling = NullValueHandling.Ignore)]
    public int? receiptFeedLinesEnd { get; set; }

    [JsonProperty("receiptFontSizeLarge", NullValueHandling = NullValueHandling.Ignore)]
    public int? receiptFontSizeLarge { get; set; }

    [JsonProperty("receiptFontSizeNormal", NullValueHandling = NullValueHandling.Ignore)]
    public int? receiptFontSizeNormal { get; set; }

    [JsonProperty("receiptFooterImage", NullValueHandling = NullValueHandling.Ignore)]
    public object receiptFooterImage { get; set; }

    [JsonProperty("receiptFooterTextLines", NullValueHandling = NullValueHandling.Ignore)]
    public object receiptFooterTextLines { get; set; }

    [JsonProperty("receiptHeaderImage", NullValueHandling = NullValueHandling.Ignore)]
    public object receiptHeaderImage { get; set; }

    [JsonProperty("receiptHeaderTextLines", NullValueHandling = NullValueHandling.Ignore)]
    public object receiptHeaderTextLines { get; set; }

    [JsonProperty("receiptLayout", NullValueHandling = NullValueHandling.Ignore)]
    public string receiptLayout { get; set; }

    [JsonProperty("receiptLetterSpacingCondensed", NullValueHandling = NullValueHandling.Ignore)]
    public decimal? receiptLetterSpacingCondensed { get; set; }

    [JsonProperty("receiptLetterSpacingNormal", NullValueHandling = NullValueHandling.Ignore)]
    public int? receiptLetterSpacingNormal { get; set; }

    [JsonProperty("receiptOpenCashDrawer", NullValueHandling = NullValueHandling.Ignore)]
    public string receiptOpenCashDrawer { get; set; }

    [JsonProperty("receiptSplitMaxHeight", NullValueHandling = NullValueHandling.Ignore)]
    public object receiptSplitMaxHeight { get; set; }

    [JsonProperty("receiptWidth", NullValueHandling = NullValueHandling.Ignore)]
    public int? receiptWidth { get; set; }

    [JsonProperty("receiptsDelay", NullValueHandling = NullValueHandling.Ignore)]
    public int? receiptsDelay { get; set; }

    [JsonProperty("runUi", NullValueHandling = NullValueHandling.Ignore)]
    public bool? runUi { get; set; }

    [JsonProperty("scaleDeviceName", NullValueHandling = NullValueHandling.Ignore)]
    public object scaleDeviceName { get; set; }

    [JsonProperty("scaleDeviceRs232BaudRate", NullValueHandling = NullValueHandling.Ignore)]
    public int? scaleDeviceRs232BaudRate { get; set; }

    [JsonProperty("scaleDeviceRs232DataBits", NullValueHandling = NullValueHandling.Ignore)]
    public int? scaleDeviceRs232DataBits { get; set; }

    [JsonProperty("scaleDeviceRs232HardwareFlowControl", NullValueHandling = NullValueHandling.Ignore)]
    public int? scaleDeviceRs232HardwareFlowControl { get; set; }

    [JsonProperty("scaleDeviceRs232Parity", NullValueHandling = NullValueHandling.Ignore)]
    public int? scaleDeviceRs232Parity { get; set; }

    [JsonProperty("scaleDeviceRs232StopBits", NullValueHandling = NullValueHandling.Ignore)]
    public int? scaleDeviceRs232StopBits { get; set; }

    [JsonProperty("scaleProtocol", NullValueHandling = NullValueHandling.Ignore)]
    public object scaleProtocol { get; set; }

    [JsonProperty("syncReceipts", NullValueHandling = NullValueHandling.Ignore)]
    public bool? syncReceipts { get; set; }

    [JsonProperty("vpfrCertificateAddress", NullValueHandling = NullValueHandling.Ignore)]
    public object vpfrCertificateAddress { get; set; }

    [JsonProperty("vpfrCertificateBusinessName", NullValueHandling = NullValueHandling.Ignore)]
    public object vpfrCertificateBusinessName { get; set; }

    [JsonProperty("vpfrCertificateCity", NullValueHandling = NullValueHandling.Ignore)]
    public object vpfrCertificateCity { get; set; }

    [JsonProperty("vpfrCertificateCountry", NullValueHandling = NullValueHandling.Ignore)]
    public object vpfrCertificateCountry { get; set; }

    [JsonProperty("vpfrCertificateSerialNumber", NullValueHandling = NullValueHandling.Ignore)]
    public object vpfrCertificateSerialNumber { get; set; }

    [JsonProperty("vpfrCertificateShopName", NullValueHandling = NullValueHandling.Ignore)]
    public object vpfrCertificateShopName { get; set; }

    [JsonProperty("vpfrClientCertificateBase64", NullValueHandling = NullValueHandling.Ignore)]
    public object vpfrClientCertificateBase64 { get; set; }

    [JsonProperty("vpfrClientCertificatePassword", NullValueHandling = NullValueHandling.Ignore)]
    public object vpfrClientCertificatePassword { get; set; }

    [JsonProperty("vpfrEnabled", NullValueHandling = NullValueHandling.Ignore)]
    public bool? vpfrEnabled { get; set; }

    [JsonProperty("vpfrUrl", NullValueHandling = NullValueHandling.Ignore)]
    public object vpfrUrl { get; set; }
}

namespace ClinicQaAgent.Services;

/// <summary>
/// Azure OpenAI 連線設定。
/// 讀取優先序: 設定檔 (appsettings.json 的 "AzureOpenAI" 區段) → 環境變數。
///
/// 認證方式:
/// - 有填 ApiKey       → 以金鑰認證。
/// - 沒填 ApiKey       → 以 Microsoft Entra ID (keyless / DefaultAzureCredential) 認證,
///   適用於資源停用金鑰 (disableLocalAuth=true) 的情境; 本機可靠 az login 身分。
///
/// Endpoint 與 DeploymentName 任一為空時, 服務會自動退回 (fallback) 到本地 mock 模式。
/// </summary>
public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? DeploymentName { get; set; }

    /// <summary>
    /// Endpoint 與 DeploymentName 都存在時, 才算「已設定 Azure OpenAI」。
    /// ApiKey 為選用: 留空則改走 Entra ID (keyless) 認證。
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(DeploymentName);

    /// <summary>
    /// 是否以金鑰認證 (有填 ApiKey); 否則使用 Entra ID (keyless)。
    /// </summary>
    public bool UseApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// 從 IConfiguration 載入設定, 並以環境變數作為後援 (fallback)。
    /// 支援的環境變數: AZURE_OPENAI_ENDPOINT / AZURE_OPENAI_API_KEY / AZURE_OPENAI_DEPLOYMENT。
    /// </summary>
    public static AzureOpenAiOptions Load(IConfiguration configuration)
    {
        var options = new AzureOpenAiOptions();
        configuration.GetSection(SectionName).Bind(options);

        // 設定檔沒填時, 改抓環境變數 (方便 CI / 容器 / 一次性 demo)。
        options.Endpoint = FirstNonEmpty(options.Endpoint, configuration["AZURE_OPENAI_ENDPOINT"]);
        options.ApiKey = FirstNonEmpty(options.ApiKey, configuration["AZURE_OPENAI_API_KEY"]);
        options.DeploymentName = FirstNonEmpty(options.DeploymentName, configuration["AZURE_OPENAI_DEPLOYMENT"]);

        return options;
    }

    private static string? FirstNonEmpty(string? primary, string? fallback) =>
        string.IsNullOrWhiteSpace(primary) ? fallback : primary;
}

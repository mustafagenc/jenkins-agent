namespace JenkinsAgent.Models
{
    /// <summary>
    /// Jenkins metrik bilgisi
    /// </summary>
    public class JenkinsMetric
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }
}
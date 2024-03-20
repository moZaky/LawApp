using System.Net;
using Serilog.Core;
using Serilog.Events;

namespace MZ.Law.App;

public class MZEnricher:ILogEventEnricher
{
    private readonly string _application;
    private readonly string _module;
    private readonly string _host;
    private readonly string _ip;

    public MZEnricher(string application, string module)
    {
        _application = application;
        _module = module;
        _host = Dns.GetHostName();
        _ip = string.Join(",", Dns.GetHostAddresses(_host)
            .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork));
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(new LogEventProperty("Application", new ScalarValue(_application)));
        logEvent.AddPropertyIfAbsent(new LogEventProperty("Module", new ScalarValue(_module)));
        logEvent.AddPropertyIfAbsent(new LogEventProperty("Host", new ScalarValue(_host)));
        logEvent.AddPropertyIfAbsent(new LogEventProperty("IP", new ScalarValue(_ip)));
    }
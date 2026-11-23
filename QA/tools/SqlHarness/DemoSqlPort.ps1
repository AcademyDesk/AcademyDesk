param([switch]$InteractiveDemo)

function Get-QaSqlPublish {
    param([bool]$InteractiveDemo = $false)
    if (-not $InteractiveDemo) { return '127.0.0.1::1433' }
    # Docker reassigns an empty HostPort after stop/start. The long-lived owned
    # demo must keep its API's SQL address while bounded validation pauses SQL.
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        $port = $listener.LocalEndpoint.Port
        if ($port -le 1024 -or $port -gt 65535) { throw 'Invalid demo SQL port.' }
        return "127.0.0.1:${port}:1433"
    } finally { $listener.Stop() }
    # Selection and Docker binding are separate; a racing listener must cause
    # Docker startup to fail. Never stop another process or fall back publicly.
}

if ($MyInvocation.InvocationName -ne '.') {
    Get-QaSqlPublish -InteractiveDemo $InteractiveDemo.IsPresent
}

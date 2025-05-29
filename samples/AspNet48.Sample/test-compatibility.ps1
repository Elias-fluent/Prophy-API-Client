# Test .NET Framework 4.8 compatibility after removing rate limiting dependencies
Write-Host "Testing .NET Framework 4.8 compatibility..."

try {
    # Try to load the updated Prophy.ApiClient from bin directory
    $prophyClientPath = "bin\\Prophy.ApiClient.dll"
    if (Test-Path $prophyClientPath) {
        $assembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path $prophyClientPath))
        Write-Host "[OK] Successfully loaded Prophy.ApiClient.dll"
        Write-Host "    Version: $($assembly.GetName().Version)"
        
        # Try to create an instance of ProphyApiClient
        $clientType = $assembly.GetType("Prophy.ApiClient.ProphyApiClient")
        if ($clientType) {
            Write-Host "[OK] Found ProphyApiClient type"
            
            # Try to instantiate it
            $client = [System.Activator]::CreateInstance($clientType, @("test-api-key", "test-org"))
            if ($client) {
                Write-Host "[OK] Successfully created ProphyApiClient instance!"
                Write-Host "SUCCESS: .NET Framework 4.8 compatibility verified!"
                Write-Host "The rate limiting dependency issue has been resolved."
            } else {
                Write-Host "[ERROR] Failed to create ProphyApiClient instance"
                exit 1
            }
        } else {
            Write-Host "[ERROR] ProphyApiClient type not found"
            exit 1
        }
    } else {
        Write-Host "[ERROR] Prophy.ApiClient.dll not found at: $prophyClientPath"
        exit 1
    }
    
} catch {
    Write-Host "[ERROR] Test failed: $($_.Exception.Message)"
    Write-Host "Exception type: $($_.Exception.GetType().FullName)"
    if ($_.Exception.InnerException) {
        Write-Host "Inner exception: $($_.Exception.InnerException.Message)"
    }
    exit 1
} 
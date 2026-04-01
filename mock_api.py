from datetime import datetime
from typing import List, Optional

from fastapi import FastAPI, Header
from pydantic import BaseModel
import uvicorn


class ExtensionInfo(BaseModel):
    extensionId: str
    extensionName: Optional[str] = ""
    extensionVersion: Optional[str] = ""
    browserType: Optional[str] = ""


class DeviceReportPayload(BaseModel):
    deviceId: str
    enrollmentToken: str
    machineName: str
    osVersion: Optional[str] = ""
    lastPolicyAppliedStatus: bool = True
    lastErrorMessage: Optional[str] = None
    extensions: List[ExtensionInfo] = []


class DevicePolicyResponse(BaseModel):
    forceInstallChrome: List[str] = []
    blockChrome: List[str] = []
    forceInstallEdge: List[str] = []
    blockEdge: List[str] = []
    allowChrome: List[str] = []
    allowEdge: List[str] = []


app = FastAPI(title="EndpointAgent Mock API", version="1.0")


@app.post("/api/device/report", response_model=DevicePolicyResponse)
def report_device(payload: DeviceReportPayload, x_enrollment_token: Optional[str] = Header(default=None)):
    print("\n" + "=" * 80)
    print(f"[{datetime.now().isoformat()}] Device report alindi")
    print(f"DeviceId: {payload.deviceId}")
    print(f"MachineName: {payload.machineName}")
    print(f"OS: {payload.osVersion}")
    print(f"LastPolicyAppliedStatus: {payload.lastPolicyAppliedStatus}")
    print(f"LastErrorMessage: {payload.lastErrorMessage}")
    print(f"Payload EnrollmentToken: {payload.enrollmentToken}")
    print(f"Header X-Enrollment-Token: {x_enrollment_token}")
    print(f"Extensions Count: {len(payload.extensions)}")
    for ext in payload.extensions:
        print(
            f"- [{ext.browserType}] {ext.extensionId} | "
            f"Name={ext.extensionName} | Version={ext.extensionVersion}"
        )
    print("=" * 80)

    # Agent entegrasyon testinde geri dönecek örnek politika seti
    return DevicePolicyResponse(
        forceInstallChrome=["cjpalhdlnbpafiamejdnhcphjbkeiagm"],
        blockChrome=["aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"],
        forceInstallEdge=["odfafepnkmbhccpbejgmiehpchacaeak"],
        blockEdge=["bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"],
        allowChrome=["ghbmnnjooekpmoecnnnilnnbdlolhkhi"],
        allowEdge=["microsoft_edge_example_extension_id"]
    )


if __name__ == "__main__":
    # Calistirma:
    # pip install fastapi uvicorn pydantic
    # python mock_api.py
    uvicorn.run("mock_api:app", host="0.0.0.0", port=5000, reload=False)


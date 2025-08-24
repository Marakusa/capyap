import './App.css';
import { useState, CSSProperties } from "react";
import { GridLoader } from "react-spinners";

const override: CSSProperties = {
  display: "block",
  margin: "0 24px 0 8px",
  borderColor: "red",
};

export default function Uploading() {
    let [loading, setLoading] = useState(true);
    let [error, setError] = useState<string | null>(null);

    window.electron?.ipcRenderer.onUploadFailed((message: string) => {
        setError(message);
        setLoading(false);
    });
    
    return (
        <>
            {
                error ? (
                    <>
                        <div className="uploadingPanelError">
                            <h3>Upload failed</h3>
                            <p>{error}</p>
                        </div>
                        <div className="uploadingErrorCountdown"></div>
                    </>
                ) : (
                    <div className="uploadingPanel">
                        <GridLoader
                            color={"#000000"}
                            loading={loading}
                            cssOverride={override}
                            size={8}
                            aria-label="Loading Spinner"
                            data-testid="loader"
                        />
                        <p>Please wait, uploading screenshot...</p>
                    </div>
                )
            }
        </>
    );
};
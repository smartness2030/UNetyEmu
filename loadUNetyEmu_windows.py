# ----------------------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ----------------------------------------------------------------------

# Libraries
import argparse
import os  # For file path handling
import platform  # For OS detection
import subprocess  # For launching Unity executable
import sys
import urllib.request
import zipfile

# ----------------------------------------------------------------------

# ROS-TCP defaults (VM: use hostname -I). Override with: python loadUNetyEmu_windows.py --ip <IP>
DEFAULT_ROS_TCP_IP = "127.0.0.1"
DEFAULT_ROS_TCP_PORT = "10000"

# Unity executable relative path
EXECUTABLE_NAME = "built_up_Windows/UNetyEmu.exe"

# GitHub Release download URL
DOWNLOAD_URL = (
    "https://github.com/intrig-unicamp/UNetyEmu/releases/download/sbrc26-release/"
    "smallcity1-windows.zip"
)

# Archive name after download
ARCHIVE_NAME = "smallcity1-windows.zip"

# ----------------------------------------------------------------------

# Function to download and extract the Unity build from GitHub Release
def download_build(dest_folder):

    # Check if the build already exists
    print("\nBuild not found. Downloading from GitHub Release...")

    # Create the destination folder if it doesn't exist
    archive_path = os.path.join(dest_folder, ARCHIVE_NAME)

    # Download the archive from the specified URL
    urllib.request.urlretrieve(DOWNLOAD_URL, archive_path)

    # Check if the download was successful
    print("Download complete.")
    print("Extracting build...")

    # Extract the downloaded archive to the destination folder
    with zipfile.ZipFile(archive_path, "r") as zf:
        zf.extractall(dest_folder)

    # Remove the downloaded archive after extraction
    os.remove(archive_path)

    # Print completion message
    print("Extraction complete.\n")

# ----------------------------------------------------------------------

# Launch the Unity application
def launch_unity(ros_ip: str, ros_port: str):

    # Detect the operating system
    system = platform.system()

    # Check if the operating system is supported
    if system != "Windows":
        raise Exception("Unsupported operating system")

    # Get the directory of the current script
    script_dir = os.path.dirname(os.path.abspath(__file__))

    # Construct the full path to the Unity executable
    UNITY_EXE = os.path.join(script_dir, EXECUTABLE_NAME)

    # Download build if not present
    if not os.path.exists(UNITY_EXE):
        download_build(script_dir)

    # Check if the file exists
    if not os.path.exists(UNITY_EXE):
        raise FileNotFoundError(f"Executable not found in: {UNITY_EXE}")

    # Launch Unity executable
    env = os.environ.copy()

    # Set the ROS-TCP endpoint environment variables to the provided IP and port
    env["UNETY_ROS_IP"] = ros_ip
    env["UNETY_ROS_TCP_PORT"] = ros_port

    # Optional: new process group so the player can receive Ctrl+C independently when supported
    creationflags = 0
    if hasattr(subprocess, "CREATE_NEW_PROCESS_GROUP"):
        creationflags = subprocess.CREATE_NEW_PROCESS_GROUP

    print(
        f"\nLaunching Unity executable:\n{UNITY_EXE}\n"
        f"ROS TCP → {env['UNETY_ROS_IP']}:{env['UNETY_ROS_TCP_PORT']}\n"
    )
    process = subprocess.Popen(
        [UNITY_EXE], env=env, cwd=script_dir, creationflags=creationflags
    )
    try:
        process.wait()
    except KeyboardInterrupt:
        print("\nCtrl+C detected. Closing Unity...")
        process.terminate()
        process.wait(timeout=15)
        print("Unity closed.")
        sys.exit(0)

# ----------------------------------------------------------------------

# Main routine to run communication with Unity
def main():
    parser = argparse.ArgumentParser(
        description="Lanza el build Windows de UNetyEmu con UNETY_ROS_IP / UNETY_ROS_TCP_PORT para ROS-TCP."
    )
    parser.add_argument(
        "--ip",
        default=DEFAULT_ROS_TCP_IP,
        help=f"Dirección del endpoint ROS-TCP (por defecto: {DEFAULT_ROS_TCP_IP}).",
    )
    parser.add_argument(
        "--port",
        default=DEFAULT_ROS_TCP_PORT,
        help=f"Puerto TCP (por defecto: {DEFAULT_ROS_TCP_PORT}).",
    )
    args = parser.parse_args()
    launch_unity(args.ip.strip(), str(args.port).strip())

# ----------------------------------------------------------------------

# Main function to run the script
if __name__ == "__main__":
    main()

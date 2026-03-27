# Libraries
import subprocess # For launching Unity executable
import platform # For OS detection
import os # For file path handling
import urllib.request
import tarfile

# -------------------------------------------------

# Unity executable relative path
EXECUTABLE_NAME = "built_up_UNetyEmuROS/smallcity1.x86_64"

# GitHub Release download URL
DOWNLOAD_URL = "https://github.com/mauriciojrcesen/UNetyEmuROS/releases/download/v1.0/smallcity1-linux.tar.gz"

# Archive name after download
ARCHIVE_NAME = "smallcity1-linux.tar.gz"

# -------------------------------------------------

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
    with tarfile.open(archive_path, "r:gz") as tar:
        tar.extractall(dest_folder)

    # Remove the downloaded archive after extraction
    os.remove(archive_path)

    # Print completion message
    print("Extraction complete.\n")
    
# -------------------------------------------------

# Function to check and set execute permissions for the Unity executable
def secure_permission_to_execute(path):
    
    # Check if the file have execute permissions
    if not os.access(path, os.X_OK):
        print(f"Adding execute permission to: {path}")
        subprocess.run(["chmod", "+x", path], check=True)

# -------------------------------------------------

# Launch the Unity application
def launch_unity():
    
    # Detect the operating system
    system = platform.system()
    
    # Check if the operating system is supported
    if system != "Linux":
        raise Exception("Unsupported operating system")
    
    # Get the directory of the current script
    script_dir = os.path.dirname(os.path.abspath(__file__))

    # Construct the full path to the Unity executable
    UNITY_EXE = os.path.join(script_dir, EXECUTABLE_NAME)
    
    # Download build if not present
    if not os.path.exists(UNITY_EXE):
        download_build(script_dir)

    # Check if the file has execute permissions
    secure_permission_to_execute(UNITY_EXE)

    # Check if the file exists
    if not os.path.exists(UNITY_EXE):
        raise FileNotFoundError(f"Executable not found in: {UNITY_EXE}")

    # Launch Unity executable
    print(f"\nLaunching Unity executable:\n{UNITY_EXE}\n")
    return subprocess.Popen(UNITY_EXE)

# -------------------------------------------------

# Main routine to run communication with Unity
def main():
    
    # Launch Unity
    launch_unity()

# -------------------------------------------------

# Main function to run the script
if __name__ == "__main__":
    main()

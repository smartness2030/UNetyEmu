################################################################################
# Copyright 2025 INTRIG
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
################################################################################

# Libraries
import os
import csv
import matplotlib.pyplot as plt
from datetime import datetime

# -----------------------------------------------------------------------------------------------------

# CSV file names
csv_filenames = ["DRO001A_rssi", "DRO002B_rssi", "CAR003C_rssi"]

# -----------------------------------------------------------------------------------------------------

# Function to read RSSI CSV files
def read_rssi_files(csv_filenames):
    """Reads specified CSV files and extracts RSSI data."""
    rssi_data = {}
    file_path = os.getcwd() + "/"

    for csv_filename in csv_filenames:
        csv_file = file_path + csv_filename + ".csv"
        if os.path.exists(csv_file):
            with open(csv_file, newline='') as csvfile:
                reader = csv.reader(csvfile)
                next(reader)  # Skip the header
                rssi_values = [int(row[1]) for row in reader if row]
                rssi_data[csv_filename] = rssi_values
        else:
            print(f"File not found: {csv_file}")
    return rssi_data

# -----------------------------------------------------------------------------------------------------

# Function to plot RSSI data
def plot_rssi_data(rssi_data, names, output_image_path):
    """Plot RSSI data for all selected files and save the image."""
    plt.figure(figsize=(10, 6))  # Set the figure size

    # Plot a line for each file
    for (name, rssi_values), filename in zip(rssi_data.items(), names):
        plt.plot(range(len(rssi_values)), rssi_values, label=filename)

    # Customize plot
    plt.title('RSSI Data during drone flight')
    plt.xlabel('Time (s)')
    plt.ylabel('RSSI (dBm)')
    plt.legend()
    plt.grid(True)
    plt.tick_params(axis='x')
    plt.tick_params(axis='y')

    # Get the current timestamp for the filename
    timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')

    # Create the filename with the prefix "RSSI" and the timestamp
    output_image_final_path = f"RSSI_{timestamp}"

    # Save the plot as an image
    plt.savefig(output_image_final_path+'.png', dpi=300)
    plt.show()

    print(f"Plot saved as {output_image_final_path}")

# -----------------------------------------------------------------------------------------------------

# Main function
def main():
    
    # Initialize the list of RSSI times
    rssi_time = []

    # Get the drone names from the filenames
    droneNames = [filename.split('_')[0] for filename in csv_filenames]
    
    # Iterate over each filename
    for filename in csv_filenames:
        # Get everything after the word "rssi"
        # First, find the position of "rssi" and then take everything after it
        rssi_index = filename.find('rssi')
        rssi_time.append(filename[rssi_index:])

    output_image_path = rssi_time[0]  # Output image path

    # Step 1: Read specified RSSI CSV files
    rssi_data = read_rssi_files(csv_filenames)

    # Step 2: Plot the RSSI data and save as an image
    plot_rssi_data(rssi_data, droneNames, output_image_path)

# -----------------------------------------------------------------------------------------------------

# Entry point for the script
if __name__ == "__main__":
    main()

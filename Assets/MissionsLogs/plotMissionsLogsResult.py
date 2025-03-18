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
import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
import os
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

# -----------------------------------------------------------------------------------------------------

# CSV file name
csv_filename = "MissionsLogs_20250311_103305"

# Set the radius to draw the drone pads
dronePadsRadius = 20

# -----------------------------------------------------------------------------------------------------

# CSV file path
file_path = os.getcwd() + "/"
csv_file = file_path + csv_filename + ".csv"

# Read the CSV file into a DataFrame
df = pd.read_csv(csv_file)

# Display the first few rows of the DataFrame
print(df.head())

# -----------------------------------------------------------------------------------------------------

# Replace NaN values in MissionId with "NoMission"
df['MissionId'] = df['MissionId'].fillna('NoMission')

# Replace NaN values in MissionStatus with "NoMissionStatus"
df['MissionStatus'] = df['MissionStatus'].fillna('NoMissionStatus')

# Remove rows with MissionId 'NoMission'
df = df[df['MissionId'] != 'NoMission']

# Categorize MissionId in the order they appear in the CSV
df['MissionId'] = pd.Categorical(df['MissionId'], categories=df['MissionId'].unique(), ordered=True)

# Convert CurrentTime to datetime for better plotting
df['CurrentTime'] = pd.to_datetime(df['CurrentTime'], format='%H:%M:%S', errors='coerce')

# Sort data by MissionId, PlayerName, and CurrentTime
df.sort_values(by=['MissionId', 'PlayerName', 'CurrentTime'], inplace=True)

# Calculate battery consumption for each state by taking the absolute difference in BatteryLevel
df['BatteryConsumption'] = df.groupby(['MissionId', 'PlayerName'], observed=False)['BatteryLevel'].diff().abs()

# Fill NaN values (which occur in the first row of each group) with 0
df['BatteryConsumption'] = df['BatteryConsumption'].fillna(0)

# Group by MissionId, PlayerName, and CurrentState to get total battery consumption per state
battery_usage = df.groupby(['MissionId', 'PlayerName', 'CurrentState'], observed=False)['BatteryConsumption'].sum().reset_index()

# Pivot the table for easier plotting
pivot_table = battery_usage.pivot_table(index=['PlayerName'], columns='CurrentState', values='BatteryConsumption', fill_value=0, observed=False)

# -----------------------------------------------------------------------------------------------------

# Predefined colors for each state
classic_colors = ['red', 'blue', 'green', 'brown', 'orange', 'purple', 'black', 'pink', 'magenta', 'yellow', 'cyan']
classic_colors_dronePads = ['blue', 'orange', 'red']

# Assign colors to each state
state_colors = {
    'StandBy': classic_colors[0],
    'TakeOff': classic_colors[1],
    'MoveToPickupPackage': classic_colors[2],
    'MoveToCheckPoint': classic_colors[3],
    'MoveToDelivery': classic_colors[4],
    'Land': classic_colors[5],
    'PickUpPackage': classic_colors[6],
    'DeliverPackage': classic_colors[7],
    'ReturnToHub': classic_colors[8]
}
   
# -----------------------------------------------------------------------------------------------------

# Function to create a 3D circle for the DronePads
def plot_3d_circle(ax, center, radius, color, num_points=100):
    
    theta = np.linspace(0, 2 * np.pi, num_points) # Vector to define the circle
    x = center[0] + radius * np.cos(theta) # 2D circle coordinates
    y = center[1] + radius * np.sin(theta)
    z = np.full_like(x, center[2]) # Keeps the circle flat (in the XY plane)

    ax.plot(x, y, z, color=color, linewidth=2) # Draw the edge of the circle

    # Fill and plot the circle
    verts = [list(zip(x, y, z))]
    ax.add_collection3d(Poly3DCollection(verts, facecolors=color, alpha=0.3))

# Define the legend for the drone pads
dronePad_legend = [
    ('blue', 'DronePad start and end'),
    ('orange', 'DronePad for pickup'),
    ('red', 'DronePad for delivery')
]

# -----------------------------------------------------------------------------------------------------

# Function for finding local minimums
def find_minimum_locations(v):
    local_minimums = []
    local_minimums_idx = []
    # Iterate from the end to the beginning of the vector
    for i in range(len(v)-2, 0, -1):
        if i == len(v)-2:
            local_minimums_idx.append(i+1)
            local_minimums.append(v[i+1])
        else:
            if v[i] <= v[i+1] and v[i] < v[i-1]:
                if v[i] not in local_minimums:
                    local_minimums_idx.append(i)
                    local_minimums.append(v[i])
                    if len(local_minimums) == 3:
                        break  # Find the first 3 local minimums
    # Sort the local minimums by altitude
    local_minimums_idx[1], local_minimums_idx[2] = local_minimums_idx[2], local_minimums_idx[1]
    return local_minimums_idx

# -----------------------------------------------------------------------------------------------------

# Create a wide figure for the 3D plot
fig = plt.figure(figsize=(12, 6))
ax = fig.add_subplot(111, projection='3d')

# Initialize color index and legend handles
color_idx = 0
legend_handles = []

# Plot trajectories for each MissionId
for mission_id in df['MissionId'].unique():
    
    # Skip "NoMission" for now, plot later
    if mission_id == 'NoMission':
        continue

    # Filter data for the current MissionId
    mission_group = df[df['MissionId'] == mission_id]
    
    # Plot trajectories for each player in the current MissionId
    for player_name, player_group in mission_group.groupby('PlayerName'):
        
        # Assign a color to the player
        color = classic_colors[color_idx % len(classic_colors)]
        
        # Increment the color index
        color_idx += 1

        # Plot the player's trajectory
        line, = ax.plot(player_group['Longitude'], player_group['Latitude'], player_group['Altitude'],
            color=color, marker='o', markersize=3, linestyle='-', linewidth=1.5, alpha=0.7)
        
        # Add the player's name and MissionId to the legend
        legend_handles.append((line, f'{player_name}'))
    
    # Filter data for the altitude, longitude, and latitude of the current MissionId
    altitudes = mission_group['Altitude'].values
    longitudes = mission_group['Longitude'].values
    latitudes = mission_group['Latitude'].values
    
    # Plot the drone pads for the current MissionId, if possible
    try:
        
        # Find the local minimums for the drone pads
        local_minimums = find_minimum_locations(altitudes)
        
        # Plot the drone pads
        for i, idx in enumerate(local_minimums):
            
            # Identify the drone pad by its altitude
            center = (longitudes[idx], latitudes[idx], altitudes[idx])
            plot_3d_circle(ax, center, radius=dronePadsRadius, color=classic_colors_dronePads[i])
            
            # Add the legend for the drone pads only once
            if i < len(dronePad_legend):  # Check to avoid index errors
                color, label = dronePad_legend[i]
                legend_handles.append((plt.Line2D([0], [0], marker='o', color='w', markerfacecolor=color, markersize=10), label))    
    except:
        pass

# Set axis labels and title
ax.set_xlabel('\nLongitude')
ax.set_ylabel('\nLatitude')
ax.set_zlabel('Altitude')
ax.set_title('3D Drone Trajectories')
ax.tick_params(axis='x')
ax.tick_params(axis='y')
ax.tick_params(axis='z')

# Add legend outside the plot
ax.legend(*zip(*legend_handles), bbox_to_anchor=(1.05, 1), loc='upper left')

# Adjust margins to fit the legend
plt.tight_layout()
fig.subplots_adjust(right=0.75)

# Save the plot as a PNG file
plt.savefig(csv_filename+'_Fig1'+'.png', dpi=300)

# -----------------------------------------------------------------------------------------------------

# Filter by player name, current time, and altitude
df = df[['PlayerName', 'CurrentTime', 'Altitude']]

# Create a new column for the relative time
df['RelativeTime'] = (pd.to_datetime(df['CurrentTime'], format='%H:%M:%S') - pd.to_datetime(df['CurrentTime'].iloc[0], format='%H:%M:%S')).dt.total_seconds()

# Plot the altitude of each drone over time
plt.figure(figsize=(10, 6))
for drone in df['PlayerName'].unique():
    drone_data = df[df['PlayerName'] == drone]
    plt.plot(drone_data['RelativeTime'], drone_data['Altitude'], linewidth=2, label=drone)

# Add labels and legend
plt.title('Altitudes during drone flight')
plt.xlabel('Time (s)')
plt.ylabel('Altitude (m)')
plt.legend()
plt.grid(True, linestyle='-', color='gray', alpha=0.5)
plt.tick_params(axis='x')
plt.tick_params(axis='y')

# Save the plot as a PNG file
plt.savefig(csv_filename+'_Fig2'+'.png', dpi=300)

# -----------------------------------------------------------------------------------------------------

# Create a stacked bar plot for battery consumption
fig, ax = plt.subplots(figsize=(12, 6))

# Plot the stacked bar plot
pivot_table.plot(kind='bar', stacked=False, color=[state_colors.get(state, 'gray') for state in pivot_table.columns], ax=ax)

# Set labels and title
ax.set_ylabel('Battery Consumption (%)')
ax.set_xlabel('')
ax.set_title('Battery Consumption by Drone and Mission Status')
ax.legend(bbox_to_anchor=(1.05, 1), loc='upper left')
ax.set_xticklabels(pivot_table.index, rotation=0, ha='right')
ax.grid(True, linestyle='-', color='gray', alpha=0.5)

# Adjust layout for the bar plot
plt.tight_layout()

# Save the plot as a PNG file
plt.savefig(csv_filename+'_Fig3'+'.png', dpi=300)

# -----------------------------------------------------------------------------------------------------

# Display all plots
plt.show()

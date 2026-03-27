# ------------------------------------------------------
# Copyright 2026 INTRIG & SMARTNESS
# Licensed under Apache 2.0: http://www.apache.org/licenses/LICENSE-2.0
# ------------------------------------------------------


from setuptools import find_packages, setup
import os
from glob import glob

package_name = 'examplePackage'

setup(
    name=package_name,
    version='0.0.0',
    packages=find_packages(exclude=['test']),
    data_files=[
        ('share/ament_index/resource_index/packages',
            ['resource/' + package_name]),
        ('share/' + package_name, ['package.xml']),
        (os.path.join('share',package_name,'models'),glob('models/*.pt')),
        (os.path.join('share',package_name,'missions'),glob(os.path.join(package_name,'missions','*.json'))),
    ],
    install_requires=['setuptools'],
    zip_safe=True,
    maintainer='felipe-capovilla',
    maintainer_email='felipe.pavanello.capovilla@gmail.com',
    description='TODO: Package description',
    license='TODO: License declaration',
    extras_require={
        'test': [
            'pytest',
        ],
    },
    entry_points={
        'console_scripts': [
            "missionPublisher=examplePackage.missionPublisher:main",
            "waypointPublisher=examplePackage.waypointPublisher:main",
            "carKeyboardControl=examplePackage.carKeyboardControl:main",
            "droneKeyboardControl=examplePackage.droneKeyboardControl:main",
            "yolo_detector=examplePackage.applyYolo:main"
        ],
    },
)

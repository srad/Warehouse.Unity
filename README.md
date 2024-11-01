﻿# Warehouse.Unity

This repo was part of a System Engineering course at the Goethe University Frankfurt/Main in which the goal was to implement a AI palett detection system for a moving forklift.

For this purpose, this repo is the implementation of a probalistic world model which provides a simulation and training data for the AI model. It is based on Unity as a generative farmework and implemented in C#.

The entire system consists of two part implementation which generates training data (this repo) and further processes the training data and finally generate a hypothesis.

![training](https://github.com/user-attachments/assets/ade38b3a-00a4-497d-ad23-0ebfd441b1f8)

## Overview of the world model

This is a high level overview of the warehouse world model in which the forklift operates.

![world_model](https://github.com/user-attachments/assets/c6ef7238-afe5-4cf9-a18f-a18b715900ac)

## Material model for the paletts in the warehouse

![material_model](https://github.com/user-attachments/assets/8bab5033-f59b-4053-b703-1d32d0804211)

## Unity editor view

This view allows to setup simulation parameters for the generative model. The entire warehouse can be dynamically regenerated

![unity](https://github.com/user-attachments/assets/7806f979-131d-4743-b0fc-f7e60b40f60a)

## Top view of a scene

![scene_top](https://github.com/user-attachments/assets/0852f5a5-daca-4b79-a96d-e030d2ddb78d)

## Annotation hints

The colored materials are for illustration how every part can be tracked for training data generation with annotation meta data.

![pallet_colors](https://github.com/user-attachments/assets/68c0433a-7900-452a-a332-67cc9c074acb)

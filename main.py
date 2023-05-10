import random
import pandas as pd
import firebase_admin
from firebase_admin import credentials
from firebase_admin import db
import numpy as np
from scipy.spatial.distance import cdist
import json
import time

# FireBase Login
cred = credentials.Certificate("steamvrsoltest-firebase-adminsdk-g0klr-00d14f98a8.json")
firebase_admin.initialize_app(cred)

# setting the database ref
db_url = 'https://steamvrsoltest-default-rtdb.europe-west1.firebasedatabase.app/'
ref = db.reference('/', None, db_url)

# getting all the data from the realtime database
Dataset = ref.child('test').get()

classes = ["CallMe", "Fist", "OpenHand", "ThumbUp"]


class PNN:
    def __init__(self, sigma=1.0):
        """
        Initialize a PNN classifier with a given value of sigma.
        """
        self.sigma = sigma

    def fit(self, X_train, y_train):
        """
        Train the PNN classifier on a training set.

        X_train: numpy array, shape (n_samples, n_features)
            Training input data.
        y_train: numpy array, shape (n_samples,)
            Target values.
        """
        self.classes = np.unique(y_train)  # Get unique target classes
        self.n_classes = len(self.classes)  # Get number of target classes
        self.X_train = X_train  # Store input data
        self.y_train = y_train  # Store target values
        self.t_probs = self.get_target_probabilities()  # Compute target probabilities

    def get_target_probabilities(self):
        """
        Compute the target probabilities for each class.

        Returns:
        t_probs: list of numpy arrays, length n_classes
            Target probabilities for each class.
        """
        t_probs = []
        for i, c in enumerate(self.classes):
            X_c = self.X_train[self.y_train == c]  # Get input data for this class
            X_c_dist = cdist(X_c, X_c)  # Compute pairwise distances between data points
            np.fill_diagonal(X_c_dist, np.inf)  # Set diagonal elements to infinity
            p = np.exp(-np.square(X_c_dist) / (2 * self.sigma ** 2)).sum(axis=0)  # Compute probability density function
            t_probs.append(p / (p.sum() + np.finfo(float).eps))  # Normalize the probabilities
        return t_probs

    def predict_proba(self, X_test):
        """
        Predict class probabilities for new input data.

        X_test: numpy array, shape (n_samples, n_features)
            Input data for which to predict class probabilities.

        Returns:
        y_prob: numpy array, shape (n_samples, n_classes)
            Predicted class probabilities for each input sample.
        """
        y_prob = np.zeros((X_test.shape[0], self.n_classes))
        for i, c in enumerate(self.classes):
            X_c = self.X_train[self.y_train == c]  # Get input data for this class
            X_c_dist = cdist(X_test, X_c)  # Compute distances between input data and training data
            p = np.exp(-np.square(X_c_dist) / (2 * self.sigma ** 2))  # Compute probability density function
            y_prob[:, i] = np.dot(p, self.t_probs[i])  # Compute class probabilities
        return y_prob

    def predict(self, X_test):
        """
        Predict class labels for new input data.

        X_test: numpy array, shape (n_samples, n_features)
            Input data for which to predict class labels.

        Returns:
        y_pred: numpy array, shape (n_samples,)
            Predicted class labels for each input sample.
        """
        y_prob = self.predict_proba(X_test)  # Get predicted class probabilities
        y_pred = self.classes[np.argmax(y_prob, axis=1)]  # Choose the class with the highest probability
        certainty = np.max(y_prob, axis=1)  # Get the maximum probability (certainty) for each prediction
        return y_pred, certainty


# the finger curls from the dataset
X = np.array([d['DataSnapshot'] for d in Dataset])

# the classes from the dataset
Y = np.array([d['Identifier'] for d in Dataset])

# initiate a PNN(Sigma = 1)
pnn = PNN(sigma=1.0)

# train it on the dataset
pnn.fit(X, Y)

# Define the finger ranges for each hand gesture
finger_ranges = {
    "Fist": [(0.85, 1), (0.85, 1), (0.85, 1), (0.85, 1), (0.85, 1)],
    "ThumbUp": [(0, 0.35), (0.85, 1), (0.85, 1), (0.85, 1), (0.85, 1)],
    "CallMe": [(0, 0.35), (0.85, 1), (0.85, 1), (0.85, 1), (0, 0.35)],
    "peace": [(0.8, 1), (0, 0.35), (0, 0.35), (0.8, 1), (0.8, 1)],
    "spiderman": [(0, 0.35), (0, 0.35), (0.85, 1), (0.85, 1), (0, 0.35)],
    "ok": [(0.5, 0.7), (0.5, 0.7), (0, 0.1), (0, 0.1), (0, 0.35)]
}

# Generate a random hand gesture
def generate_random_gesture():
    # Select a random hand gesture from the finger_ranges dictionary
    gesture = random.choice(list(finger_ranges.keys()))
    # Generate a random value for each finger within the specified range
    fingers = [random.uniform(min_range, max_range) for min_range, max_range in finger_ranges[gesture]]
    # Return the gesture and finger values as a dictionary
    return {"Identifier": gesture, "DataSnapshot": fingers}

# Generate a specified number of random hand gestures
def generate_random_gestures(num_gestures):
    # Initialize an empty list to store the generated gestures
    gestures = []
    # Generate the specified number of gestures and append to the list
    for i in range(num_gestures):
        gestures.append(generate_random_gesture())
    # Convert the list of dictionaries to a pandas DataFrame
    df = pd.DataFrame(gestures)
    df = df.transpose()
    # Return the DataFrame
    return df

# Test the generator functions by generating 5 random hand gestures and printing the results
random_gestures = generate_random_gestures(50)

print(random_gestures)
random_gestures.to_excel("random_gestures.xlsx", index=False)

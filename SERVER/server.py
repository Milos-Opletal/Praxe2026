import os
import json
from flask import Flask, request, jsonify, send_from_directory

app = Flask(__name__)
DATA_FILE = 'data/data.json'

# Initialize the data directory and file if it doesn't exist
os.makedirs(os.path.dirname(DATA_FILE), exist_ok=True)
if not os.path.exists(DATA_FILE):
    with open(DATA_FILE, 'w') as f:
        json.dump({"whitelist": [], "blacklist": []}, f)


def load_data():
    with open(DATA_FILE, 'r') as f:
        return json.load(f)


def save_data(data):
    with open(DATA_FILE, 'w') as f:
        json.dump(data, f, indent=4)


@app.route('/lists', methods=['GET'])
def get_lists():
    return jsonify(load_data())


@app.route('/lists', methods=['POST'])
def update_lists():
    """Endpoint for laptops to save newly categorized apps."""
    req_data = request.json
    current_data = load_data()

    if 'whitelist' in req_data:
        current_data['whitelist'] = list(set(current_data['whitelist'] + req_data['whitelist']))
    if 'blacklist' in req_data:
        current_data['blacklist'] = list(set(current_data['blacklist'] + req_data['blacklist']))

    save_data(current_data)
    return jsonify({"status": "success"})


@app.route('/images/<filename>', methods=['GET'])
def get_image(filename):
    return send_from_directory('static', filename)


@app.route('/download/<filename>', methods=['GET'])
def download_file(filename):
    return send_from_directory('build', filename, as_attachment=True)


if __name__ == '__main__':
    # Listen on all interfaces so laptops on the network can connect
    app.run(host='0.0.0.0', port=4331)
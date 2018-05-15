#!/usr/bin/env python3

from flask import Flask
from flask import json
from flask import request
app = Flask(__name__)

@app.route("/")
def hello():
    print(request.__module__)
    return "Hello world!"

@app.route("/logrequest", methods = ['GET','POST'])
def logrequest():
    if request.method == 'GET':
        return "ECHO: GET\n"
    
    elif request.method == 'POST':
        if request.headers['Content-Type'] == 'text/plain':
            print('text')
            return "Text Message: " + str(request.data)
        elif request.headers['Content-Type'] == 'application/json':
            print(json.dumps(request.json))
            with open('ie_compatmode_http.json', 'a', newline='') as output_json:
                output_json.write(json.dumps(request.json) + "\n")
            return "JSON Message: " + json.dumps(request.json)
        else:
            print(request.json['Message'])

        return "ECHO: POST\n"


if __name__ == '__main__':
    app.run()

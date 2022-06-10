/**
 *  Ws2812
 *
 *  Copyright 2022 Tim Schneeberger
 *
 *  Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
 *  in compliance with the License. You may obtain a copy of the License at:
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software distributed under the License is distributed
 *  on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License
 *  for the specific language governing permissions and limitations under the License.
 */
include 'asynchttp_v1'
 
metadata {
	definition (name: "Ws2812", namespace: "thepbone", author: "Tim Schneeberger", cstHandler: true) {
		capability "Color"
        capability "Color Control"
        capability "Power Meter"
		capability "Switch"
		capability "Switch Level"
        capability "Refresh"
        capability "Polling"
        capability "Health Check"
        capability "Light"
        capability "Actuator"
         
        command "setColor"
        command "refresh"
	}

	tiles(scale: 2) {
		multiAttributeTile(name:"switch", type: "lighting", width: 1, height: 1, canChangeIcon: true) {
			tileAttribute("device.switch", key: "PRIMARY_CONTROL") {
				attributeState("on", label:'${name}', action:"switch.off", icon:"st.lights.philips.hue-single", backgroundColor:"#00a0dc", nextState:"turningOff")
				attributeState("off", label:'${name}', action:"switch.on", icon:"st.lights.philips.hue-single", backgroundColor:"#ffffff", nextState:"turningOn")
				attributeState("turningOn", label:'${name}', action:"switch.off", icon:"st.lights.philips.hue-single", backgroundColor:"#00a0dc", nextState:"turningOff")
				attributeState("turningOff", label:'${name}', action:"switch.on", icon:"st.lights.philips.hue-single", backgroundColor:"#ffffff", nextState:"turningOn")
			}

			tileAttribute ("device.level", key: "SLIDER_CONTROL") {
				attributeState "level", action:"switch level.setLevel"
			}

			tileAttribute ("device.color", key: "COLOR_CONTROL") {
				attributeState "color", action:"setColor"
			}
            
            tileAttribute("device.power", key: "SECONDARY_CONTROL") {
				attributeState "power", label: '${currentValue} W'
			}
		}
	}
    
   	
    standardTile("refresh", "device.refresh", inactiveLabel: false, decoration: "flat", width: 2, height: 2) {
        state "default", label:"", action:"refresh.refresh", icon:"st.secondary.refresh"
    }
        
	main(["switch"])
	details(["switch", "power", "refresh"])
}

// parse events into attributes
def parse(String description) {
	log.debug "Parsing '${description}'"
}

def on() {
	log.debug "Executing 'on'"
    setPower(true);
}

def off() {
	log.debug "Executing 'off'"
    setPower(false);
}

def setStaticColor(red, green, blue) {
    def hex = "#"+Integer.toHexString((int)(((0xFF << 24) | (red << 16) | (green << 8) | blue) & 0xffffffffL));
    def params = [
        uri: 'https://led.timschneeberger.me',
        path: '/api/segment/bed/layer/baselayer/effect',
        body: [Name: "Static",
        	   Properties: [
               			[
               				Name: "Color",
                            Value: hex
                        ]
                   ]
              ]
    ]
    log.debug params.body
    
    asynchttp_v1.post(processColorResponse, params)
}


def setBrightness(b) {
	def val = ((int)map(b, 0, 100, 0, 255)).toString()
    def params = [
        uri: 'https://led.timschneeberger.me',
        path: '/api/brightness',
        body: val
    ]
    log.debug val
    def data = [request: b]
    asynchttp_v1.post(processBrightnessResponse, params, data)
}

def setPower(on) {
    def params = [
        uri: 'https://led.timschneeberger.me',
        path: '/api/power',
        body: on ? 'true' : 'false'
    ]
    def data = [request: on]
    asynchttp_v1.post(processPowerResponse, params, data)
}

def processPowerResponse(response, data) { 
    def actualState = data['request'];

    if(!processResponse(response)) {
    	// Error
        actualState = !actualState;
    }
    
    sendEvent(name: "switch", value: actualState ? 'on' : 'off')
    pollPowerMeter()
}

def processBrightnessResponse(response, data) { 
    if(!processResponse(response)) {
    	// Error
        poll()
        return
    }
    
    sendEvent(name: "level", value: data['request'])
    pollPowerMeter()
}

def processColorResponse(response, data) { 
    if(!processResponse(response)) {
    	// Error
        poll()
        return
    }
    
    pollPowerMeter()
}

def processResponse(response) { 
    if (response.hasError()) {
        log.error "raw error response: $response.status $response.errorData"
    }
    else {
    	log.debug "raw response: $response.status $response.data"
    }
    return !response.hasError();
}

def setSaturation(percent) {
	log.debug "setSaturation($percent)"
	setColor(saturation: percent)
}

def setHue(value) {
	log.debug "setHue($value)"
	setColor(hue: value)
}

def setColor(value) {
	log.debug "setColor: ${value}"
	if (value.hex) {
		def c = value.hex.findAll(/[0-9a-fA-F]{2}/).collect { Integer.parseInt(it, 16) }
		setStaticColor(c[0], c[1], c[2])
	} else {
		def rgb = huesatToRGB(value.hue, value.saturation)
		setStaticColor(rgb[0], rgb[1], rgb[2])
	}

	sendEvent(name: "hue", value: value.hue)
    sendEvent(name: "saturation", value: value.saturation)
    
	if(value.hex) sendEvent(name: "color", value: value.hex)
	if(value.switch) sendEvent(name: "switch", value: value.switch)
}

def ping() {
	log.debug "ping().."
	refresh()
}

def refresh() {
 	log.debug "refreshing"
	poll()
}

def poll() {
 	log.debug "polling"

    def params = [
        uri: 'https://led.timschneeberger.me',
        path: '/api/power'
    ]
    asynchttp_v1.get(updatePower, params)
    
    def paramsB = [
        uri: 'https://led.timschneeberger.me',
        path: '/api/brightness'
    ]
    asynchttp_v1.get(updateBrightness, paramsB)

    pollPowerMeter()
}

def pollPowerMeter() {
    def params = [
        uri: 'https://led.timschneeberger.me',
        path: '/api/powerConsumption'
    ]
    asynchttp_v1.get(updatePowerMeter, params)
}

def updatePowerMeter(response, data){
	def state = "$response.data"
    if(!processResponse(response)) {
    	// Error
        state = "0"
    }
    
    log.debug "$state W"
	sendEvent(name: "power", value: state)
}

def updatePower(response, data){
    if(!processResponse(response)) {
    	// Error
        return;
    }
    def state = "$response.data" == 'true' ? 'on' : 'off'
    log.debug state
	sendEvent(name: "switch", value: state)
}

def updateBrightness(response, data){
    if(!processResponse(response)) {
    	// Error
        return;
    }
    def b = response.data.toInteger()
    def state = ((int)map(b, 0, 255, 0, 100))
    log.debug state
	sendEvent(name: "level", value: state)
}

def setLevel(level, rate=null) {
	log.debug "Executing 'setLevel'"
	setBrightness(level)
}

def map(value, fromSource, toSource, fromTarget, toTarget) {
	return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget
}

def rgbToHSV(red, green, blue) {
	def hex = colorUtil.rgbToHex(red as int, green as int, blue as int)
	def hsv = colorUtil.hexToHsv(hex)
	return [hue: hsv[0], saturation: hsv[1], value: hsv[2]]
}

def huesatToRGB(hue, sat) {
	def color = colorUtil.hsvToHex(Math.round(hue) as int, Math.round(sat) as int)
	return colorUtil.hexToRgb(color)
}
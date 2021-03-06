mergeInto(LibraryManager.library, {

    LoadResourceNative: function(location) {
        var locationString = Pointer_stringify(location);
        var request = new XMLHttpRequest();
        request.open('GET', locationString, false);
        request.overrideMimeType('text\/plain; charset=x-user-defined');
        request.send(null);
        
        var response = request.responseText;
        
        var pointer = _malloc(response.length);
        var dataHeap = new Uint8Array(HEAPU8.buffer, pointer, response.length);
        for (var i=0; i < response.length; i++) {
            dataHeap[i] = response.charCodeAt(i) & 0xff;
        }
        
        window.lastLoadedResourceLength = response.length;
        return pointer;
    },
    
    GetLastLoadedResourceLengthNative : function() {
        return window.lastLoadedResourceLength;
    },
    
    GetMapLocationString : function() {
        var urlString = window.location.href;
        var url = new URL(urlString);
        var mapLocation = url.searchParams.get("map");
        if (mapLocation == null) {
            mapLocation = "";
        }

        var bufferSize = lengthBytesUTF8(mapLocation) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(mapLocation, buffer, bufferSize);
        return buffer;
    },

    DownloadNative : function(name, content) {
        var jsName = Pointer_stringify(name);
        var jsContent = Pointer_stringify(content);
        
        var element = document.createElement('a');
        element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(jsContent));
        element.setAttribute('download', jsName);
    
        element.style.display = 'none';
        document.body.appendChild(element);
    
        element.click();
    
        document.body.removeChild(element);
    },
    
    UploadNative : function(objectCallbackName, methodCallbackName) {
        var jsObjectCallbackName = Pointer_stringify(objectCallbackName);
        var jsMethodCallbackName = Pointer_stringify(methodCallbackName);
        
        var element = document.createElement('input');
        element.setAttribute('type', 'file');

        element.style.display = 'none';
        document.body.appendChild(element);

        element.click();
        
        document.body.removeChild(element);

        element.addEventListener('input', function(evt) {
            var file = element.files[0];
            if (file) {
                var reader = new FileReader();

                reader.onload = function(evt) {
                    var result = reader.result;
                    SendMessage(jsObjectCallbackName, jsMethodCallbackName, result);
                };

                reader.onerror = function(evt) {
                    SendMessage(jsObjectCallbackName, jsMethodCallbackName, '');
                };

                reader.readAsText(file, "UTF-8");
            }
        });
    },
    
    PromptNative : function(message, defaultInput) {
        var jsMessage = Pointer_stringify(message);
        var jsDefaultInput = Pointer_stringify(defaultInput);
        var jsContent = prompt(jsMessage, jsDefaultInput);

        if (jsContent == null) {
            jsContent = "";
        }

        var bufferSize = lengthBytesUTF8(jsContent) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(jsContent, buffer, bufferSize);
        return buffer;
    }

});
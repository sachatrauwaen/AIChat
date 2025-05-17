define(['jquery', 'main/extension', 'main/config'], function ($, ext, cf) {
    'use strict';
    var identifier;
    var config = cf.init();

    var init = function (wrapper, util, params, callback) {
        identifier = params.identifier;
        window.dnn.initAIChat = function () {
            return {
                utility: util,
                params: params,
                moduleName: "AIChat"
            };
        };

        util.loadBundleScript('modules/satrabel.aichat/scripts/bundles/js/app.js');

        if (typeof callback === 'function') {
            callback();
        }
    };

    var load = function (params, callback) {
        if (typeof callback === 'function') {
            callback();
        }
    };

    return {
        init: init,
        load: load
    };
});

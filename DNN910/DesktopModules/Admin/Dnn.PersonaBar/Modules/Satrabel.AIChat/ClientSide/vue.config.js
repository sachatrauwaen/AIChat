module.exports = {
    //outputDir: 'c:/sacha/dnn/DNN910/DesktopModules/Admin/Dnn.PersonaBar/Modules/Satrabel.ThemeSettings/scripts/bundles/',
    outputDir: '../scripts/bundles/',

    css: {
        extract: false
    },
    filenameHashing: false,
    // delete HTML related webpack plugins
    chainWebpack: config => {
        //config.plugins.delete('html')
        config.plugins.delete('preload')
        config.plugins.delete('prefetch')
        config.optimization.delete('splitChunks')
    }
}
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

        // Add font file handling
        config.module
            .rule('fonts')
            .test(/\.(woff2?|eot|ttf|otf)(\?.*)?$/)
            .use('url-loader')
            .loader('url-loader')
            .options({
                limit: 10000,
                name: 'DesktopModules/Admin/Dnn.PersonaBar/Modules/Satrabel.AIChat/scripts/bundles/fonts/[name].[ext]'
            })
    }
}
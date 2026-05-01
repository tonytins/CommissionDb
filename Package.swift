// swift-tools-version: 6.3
// The swift-tools-version declares the minimum version of Swift required to build this package.

import PackageDescription

let package = Package(
    name: "CommissionDb",
    products: [
        .executable(name: "cdb", targets: ["CommissionDb"]),
    ],
    dependencies: [
        .package(url: "https://github.com/apple/swift-configuration.git", from: "1.0.0"),
        .package(url: "https://github.com/apple/swift-argument-parser.git", from: "1.2.0"),
        // .package(url: "https://github.com/Lighter-swift/Lighter.git", from: "1.4.0"),
    ],
    targets: [
        // Targets are the basic building blocks of a package, defining a module or a test suite.
        // Targets can depend on other targets in this package and products from dependencies.
        .executableTarget(
            name: "CommissionDb",
            dependencies: [
                .product(name: "ArgumentParser", package: "swift-argument-parser"),
                .product(name: "Configuration", package: "swift-configuration"),
                // .product(name: "Lighter", package: "lighter"),
            ]
        ),
        .testTarget(
            name: "CommissionDbTests",
            dependencies: ["CommissionDb"]
        ),
    ],
    swiftLanguageModes: [.v6]
)

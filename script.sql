-- DROP DATABASE IF EXISTS bdFiesaMexicana;
CREATE DATABASE bdFiestaMexicana;
USE bdFiestaMexicana;

CREATE TABLE categoria (
    id        INT PRIMARY KEY AUTO_INCREMENT,
    nome      VARCHAR(150) NOT NULL,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE metodo_preparo (
    id        INT PRIMARY KEY AUTO_INCREMENT,
    nome      VARCHAR(100) NOT NULL,   -- Ex: Grelhado, Assado, Frito, Cru
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Usuarios (
    id         INT PRIMARY KEY AUTO_INCREMENT,
    nome       VARCHAR(100),
    email      VARCHAR(100) UNIQUE,
    senha_hash VARCHAR(255),
    role       ENUM('Funcionario', 'Admin'),
    ativo      TINYINT(1) DEFAULT 1,
    criado_em  DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Mesa (
    id         INT PRIMARY KEY AUTO_INCREMENT,
    numero     INT NOT NULL,
    capacidade INT NOT NULL,
    status     ENUM('Livre', 'Ocupado') NOT NULL DEFAULT 'Livre',
    criado_em  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);


CREATE TABLE Garcom (
	id        INT PRIMARY KEY AUTO_INCREMENT,
    nome      VARCHAR(100) NOT NULL,
    cpf       CHAR(11) UNIQUE,
    turno     ENUM('Almoço', 'Jantar', 'Integral') NOT NULL,
    criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Prato (
    id              INT PRIMARY KEY AUTO_INCREMENT,
    nome            VARCHAR(200) NOT NULL,
    preco           DECIMAL(10,2) NOT NULL,
    descricao       TEXT,
    categoria       INT,
    metodo_preparo  INT,
    nivel_picancia  ENUM('Sem Pimenta','Suave','Médio','Forte','Extra') DEFAULT 'Sem Pimenta',
    tempo_preparo   INT COMMENT 'Tempo em minutos',
    disponivel BOOL NOT NULL,
    capa_arquivo    VARCHAR(255) NULL,
    criado_em       DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (categoria)      REFERENCES categoria(id),
    FOREIGN KEY (metodo_preparo) REFERENCES metodo_preparo(id)
);


CREATE TABLE Pedido (
    id         INT PRIMARY KEY AUTO_INCREMENT,
    mesa       INT,
    garcom     INT,
    status     ENUM('Pendente','Preparando','Finalizado','Cancelado') DEFAULT 'Pendente',
    observacao VARCHAR(255) NULL COMMENT 'Ex: sem coentro, molho à parte',
    total      DECIMAL(10,2),
    data_hora  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    criado_em  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (mesa)   REFERENCES Mesa(id),
    FOREIGN KEY (garcom) REFERENCES Garcom(id)
);

CREATE TABLE ItemPedido (
    id             INT PRIMARY KEY AUTO_INCREMENT,
    pedido         INT NOT NULL,
    prato          INT NOT NULL,
    quantidade     INT NOT NULL,
    preco_unitario DECIMAL(10,2) NOT NULL,
    subtotal       DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (pedido) REFERENCES Pedido(id),
    FOREIGN KEY (prato)  REFERENCES Prato(id)
);

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_usuario_criar $$
CREATE PROCEDURE sp_usuario_criar (
    IN p_nome VARCHAR(100),
    IN p_email VARCHAR(100),
    IN p_senha_hash VARCHAR(255),
    IN p_role VARCHAR(20)
)
BEGIN
    INSERT INTO Usuarios (nome, email, senha_hash, role, ativo, criado_em)
    VALUES (p_nome, p_email, p_senha_hash, p_role, 1, NOW());
END $$

DROP PROCEDURE IF EXISTS sp_usuario_obter_por_email $$
CREATE PROCEDURE sp_usuario_obter_por_email(IN p_email VARCHAR(100))
BEGIN
  SELECT id, nome, email, senha_hash, role, ativo
  FROM usuarios
  WHERE email = p_email
  LIMIT 1;
END $$

DELIMITER ;

-- exemplo de uso (ATENÇÃO: role deve ser 'Adm', não 'Admin')
CALL sp_usuario_criar(
 'Juan Pablo Admin',
 'juanpablo@fiesta.com',
 '$2a$11$Q91fiPYPec73pUA4DKByXeSNOZ6TYn2ZY5jWSWpr57rkfUEyKjWq2',
 'Admin'
);

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_categoria_listar $$
CREATE PROCEDURE sp_categoria_listar()
BEGIN
    SELECT id, nome FROM categoria ORDER BY nome;
END $$

DROP PROCEDURE IF EXISTS sp_metodo_preparo_listar $$
CREATE PROCEDURE sp_metodo_preparo_listar()
BEGIN
    SELECT id, nome FROM metodo_preparo ORDER BY nome;
END $$

DROP PROCEDURE IF EXISTS sp_prato_listar $$
CREATE PROCEDURE sp_prato_listar()
BEGIN
    SELECT
        p.id,
        p.nome,
        p.preco,
        p.descricao,
        p.categoria,
        c.nome AS categoria_nome, 
        p.metodo_preparo,
        mp.nome AS metodo_preparo_nome, 
        p.nivel_picancia,
        p.tempo_preparo,
        p.disponivel,
        p.capa_arquivo,
        p.criado_em
    FROM Prato p
    LEFT JOIN categoria c ON c.id = p.categoria
    LEFT JOIN metodo_preparo mp ON mp.id = p.metodo_preparo
    ORDER BY p.nome;
END $$

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_categoria_listar $$
CREATE PROCEDURE sp_categoria_listar()
BEGIN
    SELECT id, nome FROM categoria ORDER BY nome;
END $$

DROP PROCEDURE IF EXISTS sp_metodo_preparo_listar $$
CREATE PROCEDURE sp_metodo_preparo_listar()
BEGIN
    SELECT id, nome FROM metodo_preparo ORDER BY nome;
END $$

DROP PROCEDURE IF EXISTS sp_prato_listar $$
CREATE PROCEDURE sp_prato_listar()
BEGIN
    SELECT
        p.id,
        p.nome,
        p.preco,
        p.descricao,
        p.categoria,
        c.nome AS categoria_nome,
        p.metodo_preparo,
        mp.nome AS metodo_preparo_nome,
        p.nivel_picancia,
        p.tempo_preparo,
        p.disponivel,
        p.capa_arquivo,
        p.criado_em
    FROM Prato p
    LEFT JOIN categoria c ON c.id = p.categoria
    LEFT JOIN metodo_preparo mp ON mp.id = p.metodo_preparo
    ORDER BY p.nome;
END $$

-- Procedure para Criar um Prato
DROP PROCEDURE IF EXISTS sp_prato_criar $$
CREATE PROCEDURE sp_prato_criar (
    IN p_nome            VARCHAR(200),
    IN p_preco           DECIMAL(10,2),
    IN p_descricao       TEXT,
    IN p_categoria       INT,
    IN p_metodo_preparo  INT,
    IN p_nivel_picancia  ENUM('Sem Pimenta','Suave','Médio','Forte','Extra'),
    IN p_tempo_preparo   INT,
    IN p_disponivel      BOOL,
    IN p_capa_arquivo    VARCHAR(255)
)
BEGIN
    INSERT INTO Prato (
        nome, preco, descricao, categoria, metodo_preparo,
        nivel_picancia, tempo_preparo, disponivel, capa_arquivo
    )
    VALUES (
        p_nome, p_preco, p_descricao, p_categoria, p_metodo_preparo,
        p_nivel_picancia, p_tempo_preparo, p_disponivel, p_capa_arquivo
    );
END $$

DROP PROCEDURE IF EXISTS sp_prato_obter $$
CREATE PROCEDURE sp_prato_obter (
    IN p_id INT
)
BEGIN
    SELECT
        p.id,
        p.nome,
        p.preco,
        p.descricao,
        p.categoria,
        c.nome AS categoria_nome,
        p.metodo_preparo,
        mp.nome AS metodo_preparo_nome,
        p.nivel_picancia,
        p.tempo_preparo,
        p.disponivel,
        p.capa_arquivo,
        p.criado_em
    FROM Prato p
    LEFT JOIN categoria c ON c.id = p.categoria
    LEFT JOIN metodo_preparo mp ON mp.id = p.metodo_preparo
    WHERE p.id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_prato_atualizar $$
CREATE PROCEDURE sp_prato_atualizar (
    IN p_id              INT,
    IN p_nome            VARCHAR(200),
    IN p_preco           DECIMAL(10,2),
    IN p_descricao       TEXT,
    IN p_categoria       INT,
    IN p_metodo_preparo  INT,
    IN p_nivel_picancia  ENUM('Sem Pimenta','Suave','Médio','Forte','Extra'),
    IN p_tempo_preparo   INT,
    IN p_disponivel      BOOL,
    IN p_capa_arquivo    VARCHAR(255)
)
BEGIN
    UPDATE Prato
    SET
        nome            = p_nome,
        preco           = p_preco,
        descricao       = p_descricao,
        categoria       = p_categoria,
        metodo_preparo  = p_metodo_preparo,
        nivel_picancia  = p_nivel_picancia,
        tempo_preparo   = p_tempo_preparo,
        disponivel      = p_disponivel,
        capa_arquivo    = p_capa_arquivo
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_prato_excluir $$
CREATE PROCEDURE sp_prato_excluir (
    IN p_id INT
)
BEGIN
    DELETE FROM Prato
    WHERE id = p_id;
END $$

DELIMITER ;
